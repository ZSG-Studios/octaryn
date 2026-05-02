(function () {
  const keywords = new Set([
    "abstract", "as", "base", "bool", "break", "case", "catch", "class", "const",
    "continue", "default", "delegate", "do", "double", "else", "enum", "event",
    "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach",
    "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
    "namespace", "new", "null", "object", "operator", "out", "override", "params",
    "private", "protected", "public", "readonly", "record", "ref", "return",
    "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
    "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
    "unchecked", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile",
    "while"
  ]);

  const knownTypes = new Set([
    "GameModuleManifest", "GameModuleCompatibility", "GameModuleContentDeclaration",
    "GameModuleAssetDeclaration", "GameModuleScheduleDeclaration",
    "ScheduledSystemDeclaration", "ScheduledResourceAccess", "ScheduledAccessMode",
    "HostWorkPhase", "HostWorkScheduleFlags", "HostScheduleIds", "HostApiIds",
    "ModuleCapabilityIds", "FrameworkApiGroupIds", "IGameModuleRegistration",
    "IGameModuleInstance", "ModuleHostContext", "ModuleFrameContext",
    "ModuleValidationReport", "ModuleValidationIssue", "GameModuleValidator",
    "ServerSnapshotHeader", "Console"
  ]);

  function escapeHtml(value) {
    return value
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;");
  }

  function span(className, value) {
    return `<span class="token ${className}">${escapeHtml(value)}</span>`;
  }

  function readLineComment(text, index) {
    const end = text.indexOf("\n", index);
    return end === -1 ? text.length : end;
  }

  function readBlockComment(text, index) {
    const end = text.indexOf("*/", index + 2);
    return end === -1 ? text.length : end + 2;
  }

  function readString(text, index) {
    let i = index + 1;
    while (i < text.length) {
      if (text[i] === "\\") {
        i += 2;
      } else if (text[i] === text[index]) {
        return i + 1;
      } else {
        i += 1;
      }
    }
    return text.length;
  }

  function readVerbatimString(text, index) {
    let i = index + 2;
    while (i < text.length) {
      if (text[i] === "\"" && text[i + 1] === "\"") {
        i += 2;
      } else if (text[i] === "\"") {
        return i + 1;
      } else {
        i += 1;
      }
    }
    return text.length;
  }

  function highlightCSharp(text) {
    let output = "";
    let i = 0;

    while (i < text.length) {
      const ch = text[i];
      const next = text[i + 1];

      if (ch === "/" && next === "/") {
        const end = readLineComment(text, i);
        output += span("comment", text.slice(i, end));
        i = end;
        continue;
      }

      if (ch === "/" && next === "*") {
        const end = readBlockComment(text, i);
        output += span("comment", text.slice(i, end));
        i = end;
        continue;
      }

      if (ch === "@" && next === "\"") {
        const end = readVerbatimString(text, i);
        output += span("string", text.slice(i, end));
        i = end;
        continue;
      }

      if (ch === "\"" || ch === "'") {
        const end = readString(text, i);
        output += span("string", text.slice(i, end));
        i = end;
        continue;
      }

      if (/[0-9]/.test(ch)) {
        const match = text.slice(i).match(/^[0-9][0-9A-Fa-f_xX.]*/);
        output += span("number", match[0]);
        i += match[0].length;
        continue;
      }

      if (/[A-Za-z_]/.test(ch)) {
        const match = text.slice(i).match(/^[A-Za-z_][A-Za-z0-9_]*/);
        const word = match[0];
        const previous = i > 0 ? text[i - 1] : "";
        if (keywords.has(word)) {
          output += span("keyword", word);
        } else if (knownTypes.has(word) || /^[A-Z][A-Za-z0-9_]*$/.test(word)) {
          output += span("type", word);
        } else if (previous === ".") {
          output += span("member", word);
        } else {
          output += escapeHtml(word);
        }
        i += word.length;
        continue;
      }

      output += escapeHtml(ch);
      i += 1;
    }

    return output;
  }

  for (const code of document.querySelectorAll("code.language-csharp")) {
    code.innerHTML = highlightCSharp(code.textContent);
  }
})();
