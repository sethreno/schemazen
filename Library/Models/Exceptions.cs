using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SchemaZen.Library.Models;

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
	private readonly string _message;

	public DataFileException(string message, string fileName, int lineNumber) {
		_message = message;
		FileName = fileName;
		LineNumber = lineNumber;
	}

	public override string Message => _message + $" - in file named {FileName}:{LineNumber}";

	public string FileName { get; }

	public int LineNumber { get; }
}
