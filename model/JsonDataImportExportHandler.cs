using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;
using SchemaZen.Library.Models;

namespace SchemaZen.Library
{
    public class JsonDataImportExportHandler : AbstractDataImportExportHandler
    {
        public override string FileExtension => ".json";

        protected override void ExportData(SqlDataReader dr, Column[] cols, TextWriter data)
        {
            using (var writer = new JsonTextWriter(data)) {
                Configure(writer);

                writer.WriteStartArray();

                while (dr.Read())
                {
                    writer.WriteStartObject();

                    foreach (var c in cols)
                    {
                        writer.WritePropertyName(c.Name);
                        writer.WriteValue(dr[c.Name]);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
        }

        private static void Configure(JsonTextWriter writer) {
            writer.QuoteName = false;
            writer.CloseOutput = false;
            writer.Indentation = 0;
            writer.Formatting = Formatting.Indented;
        }


        private static void Configure(JsonTextReader reader)
        {
            reader.FloatParseHandling = FloatParseHandling.Decimal;
        }

        protected override void ImportData(DataTable dt, Column[] cols, SqlBulkCopy bulk, string filename)
        {
            var batch_rows = 0;

            using (var file = new StreamReader(filename))
            using (var reader = new JsonTextReader(file))
            {
                Configure(reader);
                if (reader.Read())
                {
                    if (reader.TokenType != JsonToken.StartArray) {
                        throw new DataFileException("Data file is expecting a start array token: " + reader.TokenType,
                            filename, reader.LineNumber);
                    }

                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                    {
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            throw new DataFileException("Data file is expecting a start object token: " + reader.TokenType,
                            filename, reader.LineNumber);
                        }

                        var row = dt.NewRow();

                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                        {
                            if (reader.TokenType == JsonToken.PropertyName) {
                                var propertyName = (string) reader.Value;
                                if (reader.Read()) {
                                    row[propertyName] = reader.Value ?? DBNull.Value;
                                }
                            } else {
                                throw new DataFileException(
                                    "Data file is expecting a property name token: " + reader.TokenType,
                                    filename, reader.LineNumber);
                            }
                        }

                        batch_rows++;
                        dt.Rows.Add(row);

                        if (batch_rows == RowsInBatch)
                        {
                            batch_rows = 0;
                            bulk.WriteToServer(dt);
                            dt.Clear();
                        }
                    }

                    bulk.WriteToServer(dt);
                    bulk.Close();
                }
            }
        }
    }
}