﻿namespace SchemaZen.Library.Models;

public class Role : IScriptable, INameable {
	public string Script { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		return Script;
	}
}
