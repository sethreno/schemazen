using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SchemaZen.model {
	public class BatchSqlFileException : Exception {
		public List<SqlFileException> Exceptions { get; set; }
	}

	public class SqlBatchException : Exception {
		private readonly int lineNumber;

		private readonly string message;

		public SqlBatchException(SqlException ex, int prevLinesInBatch)
			: base("", ex) {
			lineNumber = ex.LineNumber + prevLinesInBatch;
			message = ex.Message;
		}

		public int LineNumber {
			get { return lineNumber; }
		}

		public override string Message {
			get { return message; }
		}
	}

	public class SqlFileException : SqlBatchException {
		private readonly string fileName;

		public SqlFileException(string fileName, SqlBatchException ex)
			: base((SqlException) ex.InnerException, ex.LineNumber - 1) {
			this.fileName = fileName;
		}

		public string FileName {
			get { return fileName; }
		}
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

		public override string Message {
			get { return _message + string.Format(" - in file named {0}:{1}", _fileName, _lineNumber); }
		}

		public string FileName {
			get { return _fileName; }
		}

		public int LineNumber {
			get { return _lineNumber; }
		}
	}
}
