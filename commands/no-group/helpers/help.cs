namespace Commands.Helpers
{
    public class CommandInfoGroup
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Emoji { get; set; }
        public string Url { get; set; }
        public CommandInfo[] Commands { get; set; }
    }

    public class CommandInfo
    {
        public string Name { get; set; }
        public bool InheritGroupName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public ParameterInfo[] Parameters { get; set; }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}