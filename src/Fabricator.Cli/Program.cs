using Fabricator.Cli;

var rootCommand = CliCommandFactory.CreateRootCommand();
return await rootCommand.Parse(args).InvokeAsync();
