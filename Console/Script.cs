﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ManyConsole;
using SchemaZen.Library;
using SchemaZen.Library.Command;
using SchemaZen.Library.Models;

namespace SchemaZen.Console;

public class Script : BaseCommand {
	private Logger _logger;

	public Script()
		: base(
			"Script",
			"Generate scripts for the specified database.") {
		HasOption(
			"dataTables=",
			"A comma separated list of tables to export data from.",
			o => DataTables = o);
		HasOption(
			"dataTablesPattern=",
			"A regular expression pattern that matches tables to export data from.",
			o => DataTablesPattern = o);
		HasOption(
			"dataTablesExcludePattern=",
			"A regular expression pattern that exclude tables to export data from.",
			o => DataTablesExcludePattern = o);
		HasOption(
			"tableHint=",
			"Table hint to use when exporting data.",
			o => TableHint = o);
		HasOption(
			"filterTypes=",
			"A comma separated list of the types that will not be scripted. Valid types: " +
			Database.ValidTypes,
			o => FilterTypes = o);
		HasOption(
			"onlyTypes=",
			"A comma separated list of the types that will only be scripted. Valid types: " +
			Database.ValidTypes,
			o => OnlyTypes = o);
		HasOption(
			"routineList=",
			"A comma separated list of the database routines that will only be scripted.",
			o => RoutineList = o);
		HasOption(
			"tableList=",
			"A comma separated list of the database tables that will only be scripted.",
			o => TableList = o);
			}


	protected string DataTables { get; set; }
	protected string FilterTypes { get; set; }
	protected string OnlyTypes { get; set; }
	protected string DataTablesPattern { get; set; }
	protected string DataTablesExcludePattern { get; set; }
	protected string TableHint { get; set; }
	protected string TableList { get; set; }
	protected string RoutineList { get; set; }

	public override int Run(string[] args) {
		_logger = new Logger(Verbose);

		if (!Overwrite && Directory.Exists(ScriptDir)) {
			if (!ConsoleQuestion.AskYN(
				    $"{ScriptDir} already exists - do you want to replace it"))
				return 1;
			Overwrite = true;
		}

		var scriptCommand = new ScriptCommand {
			ConnectionString = ConnectionString,
			DbName = DbName,
			Pass = Pass,
			ScriptDir = ScriptDir,
			Server = Server,
			User = User,
			Logger = _logger,
			Overwrite = Overwrite,
			DisableRoles = this.DisableRoles
		};

		var filteredTypes = HandleFilteredTypes();
		var namesAndSchemas = HandleDataTables(DataTables);
		var filteredTables = HandleObjectList(TableList);
		var filteredRoutines = HandleObjectList(RoutineList);

		try {
			scriptCommand.Execute(
				namesAndSchemas,
				DataTablesPattern,
				DataTablesExcludePattern,
				TableHint,
				filteredTypes,
				filteredTables,
				filteredRoutines
				);
		} catch (Exception ex) {
			throw new ConsoleHelpAsException(ex.Message);
		}

		return 0;
	}

	private List<string> HandleFilteredTypes() {
		var removeTypes = FilterTypes?.Split(',').ToList() ?? new List<string>();
		var keepTypes = OnlyTypes?.Split(',').ToList() ?? new List<string>(Database.Dirs);

		var invalidTypes = removeTypes.Union(keepTypes).Except(Database.Dirs).ToList();
		if (invalidTypes.Any()) {
			var msg = invalidTypes.Count() > 1 ? " are not valid types." : " is not a valid type.";
			_logger.Log(TraceLevel.Warning, string.Join(", ", invalidTypes.ToArray()) + msg);
			_logger.Log(TraceLevel.Warning, $"Valid types: {Database.ValidTypes}");
		}

		return Database.Dirs.Except(keepTypes.Except(removeTypes)).ToList();
	}

	private List<string> HandleObjectList(string stringList)
	{
		if (!String.IsNullOrEmpty(stringList))
			return stringList.Split(",").ToList();
		return null;
	}

	private Dictionary<string, string> HandleDataTables(string tableNames) {
		var dataTables = new Dictionary<string, string>();

		if (string.IsNullOrEmpty(tableNames))
			return dataTables;

		foreach (var value in tableNames.Split(',')) {
			var schema = "dbo";
			var name = value;
			if (value.Contains(".")) {
				schema = value.Split('.')[0];
				name = value.Split('.')[1];
			}

			dataTables[name] = schema;
		}

		return dataTables;
	}
}
