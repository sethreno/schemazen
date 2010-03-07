namespace console {
    interface ICommand {
        bool Parse(string[] args);
        string GetUsageText();
        bool Run();
    }
}
