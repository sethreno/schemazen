using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace model
{
	public class BatchSqlFileException : Exception
	{
		public List<SqlFileException> Exceptions { get; set; }
	}

	public class SqlBatchException : Exception
	{
		private readonly int lineNumber;

		private readonly string message;

		public SqlBatchException(SqlException ex, int prevLinesInBatch)
			: base("", ex)
		{
			this.lineNumber = ex.LineNumber + prevLinesInBatch;
			this.message = ex.Message;
		}

		public int LineNumber
		{
			get { return this.lineNumber; }
		}

		public override string Message
		{
			get { return this.message; }
		}
	}

	public class SqlFileException : SqlBatchException
	{
		private readonly string fileName;

		public SqlFileException(string fileName, SqlBatchException ex)
			: base((SqlException)ex.InnerException, ex.LineNumber - 1)
		{
			this.fileName = fileName;
		}

		public string FileName
		{
			get { return this.fileName; }
		}
	}

	public class DataFileException : Exception
	{
		private readonly string _fileName;
		private readonly int _lineNumber;
		private readonly string _message;

		public DataFileException(string message, string fileName, int lineNumber)
		{
			this._message = message;
			this._fileName = fileName;
			this._lineNumber = lineNumber;
		}

		public override string Message
		{
			get { return this._message; }
		}

		public string FileName
		{
			get { return this._fileName; }
		}

		public int LineNumber
		{
			get { return this._lineNumber; }
		}
	}

	public class DataException : Exception
	{
		private readonly int _lineNumber;
		private readonly string _message;

		public DataException(string message, int lineNumber)
		{
			this._message = message;
			this._lineNumber = lineNumber;
		}

		public override string Message
		{
			get { return this._message; }
		}

		public int LineNumber
		{
			get { return this._lineNumber; }
		}
	}
}