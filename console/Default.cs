namespace console {
    class Default : ICommand {

        public bool Parse(string[] args) {
            return false;
        }
        public string GetUsageText() {
            return @"<command> [options]

Valid commands include:
   script
   create
   compare

Type schemanator <command> help for information on a specific command.
";
        }
        public bool Run() {
            return false;
        }
    }
}
