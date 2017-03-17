using System.IO;
using SchemaZen.Library.Models;

namespace SchemaZen.Library
{
    public interface IDataImportExportHandler
    {
        string FileExtension { get; }
        void ExportData(Table table, string conn, TextWriter data, string tableHint = null);
        void ImportData(Table table, string conn, string filename);
    }
}