using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SchemaZen.Library.Models {
	public class BatchSqlFileException : Exception {
		public List<SqlFileException> Exceptions { get; set; }
	}

	public class SqlBatchException : Exception {
		public SqlBatchException(SqlException ex, int prevLinesInBatch)
			: base("", ex) {
			LineNumber = ex.LineNumber + prevLinesInBatch;
			Message = ex.Message;
		}

		public int LineNumber { get; }

		public override string Message { get; }
	}

	public class SqlFileException : SqlBatchException {
		public SqlFileException(string fileName, SqlBatchException ex)
			: base((SqlException)ex.InnerException, ex.LineNumber - 1) {
			FileName = fileName;
		}

		public string FileName { get; }
	}

	public class DataFileException : Exception {
		private readonly string _fileName;
		private readonly int _lineNumber;
		private readonly string _message;

		public DataFileException(string message, string fileName, int lineNumber) {
			_message = message;
			_fileName = fileName;
			_lineNumber = lineNumber;
		}

		public override string Message => _message + $" - in file named {_fileName}:{_lineNumber}";

		public string FileName => _fileName;

		public int LineNumber => _lineNumber;
	}
}
