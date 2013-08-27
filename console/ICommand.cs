namespace console {
	internal interface ICommand {
		bool Parse(string[] args);
		string GetUsageText();
		bool Run();
	}
}