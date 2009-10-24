using System;
using System.Data.SqlClient;

namespace model {
	public class SqlBatchException : Exception {
		public SqlBatchException(SqlException ex, int prevLinesInBatch)
			: base("", ex) {
			lineNumber = ex.LineNumber + prevLinesInBatch;
			message = ex.Message;
		}

		private int lineNumber;
		public int LineNumber { get { return lineNumber; } }
		private string message;
		public string Message { get { return message; } }

	}

	public class SqlFileException : SqlBatchException {
		public SqlFileException(string fileName, SqlBatchException ex)
			: base((SqlException)ex.InnerException, ex.LineNumber - 1) {
			this.fileName = fileName;
		}

		private string fileName;
		public string FileName { get { return fileName; } }
	}

	public class DataFileException : Exception {
		private string _message;
		public string Message { get { return _message; } }
		private string _fileName;
		public string FileName { get { return _fileName; } }
		private int _lineNumber;
		public int LineNumber { get { return _lineNumber; } }

		public DataFileException(string message, string fileName, int lineNumber) {
			_message = message;
			_fileName = fileName;
			_lineNumber = lineNumber;
		}
	}

	public class DataException : Exception {
		private string _message;
		public string Message { get { return _message; } }
		private int _lineNumber;
		public int LineNumber { get { return _lineNumber; } }

		public DataException(string message, int lineNumber) {
			_message = message;
			_lineNumber = lineNumber;
		}
	}
}
