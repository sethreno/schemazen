using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using SchemaZen.Library.Models;

namespace SchemaZen.Library {
    public class TsvDataImportExportHandler : AbstractDataImportExportHandler
    {
        private const string _rowSeparator = "\r\n";
        private const string _tab = "\t";
        private const string _escapeTab = "--SchemaZenTAB--";
        private const string _carriageReturn = "\r";
        private const string _escapeCarriageReturn = "--SchemaZenCR--";
        private const string _lineFeed = "\n";
        private const string _escapeLineFeed = "--SchemaZenLF--";
        private const string _nullValue = "--SchemaZenNull--";
        private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        public override string FileExtension => ".tsv";

        protected override void ExportData(SqlDataReader dr, Column[] cols, TextWriter data) {
            while (dr.Read()) {
                foreach (var c in cols) {
                    if (dr[c.Name] is DBNull)
                        data.Write(_nullValue);
                    else if (dr[c.Name] is byte[])
                        data.Write(new SoapHexBinary((byte[]) dr[c.Name]).ToString());
                    else if (dr[c.Name] is DateTime)
                        data.Write(((DateTime) dr[c.Name]).ToString(_dateTimeFormat, CultureInfo.InvariantCulture));
                    else
                        data.Write(dr[c.Name].ToString()
                            .Replace(_tab, _escapeTab)
                            .Replace(_lineFeed, _escapeLineFeed)
                            .Replace(_carriageReturn, _escapeCarriageReturn));
                    if (c != cols.Last())
                        data.Write(_tab);
                }
                data.WriteLine();
            }
        }

        protected override void ImportData(DataTable dt, Column[] cols, SqlBulkCopy bulk, string filename) {
            var linenumber = 0;
            var batch_rows = 0;

            using (var file = new StreamReader(filename)) {
                var line = new List<char>();
                while (file.Peek() >= 0) {
                    var rowsep_cnt = 0;
                    line.Clear();

                    while (file.Peek() >= 0) {
                        var ch = (char) file.Read();
                        line.Add(ch);

                        if (ch == _rowSeparator[rowsep_cnt])
                            rowsep_cnt++;
                        else
                            rowsep_cnt = 0;

                        if (rowsep_cnt == _rowSeparator.Length) {
                            // Remove rowseparator from line
                            line.RemoveRange(line.Count - _rowSeparator.Length, _rowSeparator.Length);
                            break;
                        }
                    }
                    linenumber++;

                    // Skip empty lines
                    if (line.Count == 0)
                        continue;

                    batch_rows++;

                    var row = dt.NewRow();
                    var fields = (new String(line.ToArray())).Split(new[] {_tab}, StringSplitOptions.None);
                    if (fields.Length != dt.Columns.Count) {
                        throw new DataFileException("Incorrect number of columns", filename, linenumber);
                    }
                    for (var j = 0; j < fields.Length; j++) {
                        try {
                            row[j] = ConvertType(cols[j].Type,
                                fields[j].Replace(_escapeLineFeed, _lineFeed)
                                    .Replace(_escapeCarriageReturn, _carriageReturn)
                                    .Replace(_escapeTab, _tab));
                        } catch (FormatException ex) {
                            throw new DataFileException($"{ex.Message} at column {j + 1}", filename, linenumber);
                        }
                    }
                    dt.Rows.Add(row);

                    if (batch_rows == RowsInBatch) {
                        batch_rows = 0;
                        bulk.WriteToServer(dt);
                        dt.Clear();
                    }
                }
            }

            bulk.WriteToServer(dt);
            bulk.Close();
        }

        public static object ConvertType(string sqlType, string val) {
            if (val == _nullValue)
                return DBNull.Value;

            switch (sqlType.ToLower()) {
                case "bit":
                    //added for compatibility with bcp
                    if (val == "0") val = "False";
                    if (val == "1") val = "True";
                    return bool.Parse(val);
                case "datetime":
                case "smalldatetime":
                    return DateTime.Parse(val, CultureInfo.InvariantCulture);
                case "int":
                    return int.Parse(val);
                case "uniqueidentifier":
                    return new Guid(val);
                case "binary":
                case "varbinary":
                case "image":
                    return SoapHexBinary.Parse(val).Value;
                default:
                    return val;
            }
        }
    }
}