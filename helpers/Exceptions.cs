using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SchemaZen.model {
	public class SqlBatchException : Exception {
		private readonly int _lineNumber;

		public SqlBatchException (SqlException ex, int prevLinesInBatch) : base(ex.Message, ex) {
			_lineNumber = ex.LineNumber + prevLinesInBatch;
		}

		public int LineNumber {
			get {
				return _lineNumber;
			}
		}

	}

	public class SqlFileException : SqlBatchException {
		private readonly string _fileName;

		public SqlFileException (string fileName, SqlBatchException ex) : base((SqlException)ex.InnerException, ex.LineNumber - 1) {
			this._fileName = fileName;
		}

		public string FileName {
			get {
				return _fileName;
			}
		}
	}

	public class BatchSqlFileException : Exception {
		public List<SqlFileException> Exceptions { get; set; }
	}

	public class DataFileException : Exception {
		private readonly string _fileName;
		private readonly int _lineNumber;

		public DataFileException (string message, string fileName, int lineNumber) : base(message) {
			_fileName = fileName;
			_lineNumber = lineNumber;
		}

		public string FileName {
			get {
				return _fileName;
			}
		}

		public int LineNumber {
			get {
				return _lineNumber;
			}
		}
	}
}
