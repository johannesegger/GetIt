using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using MirrorSharp.Internal.Abstraction;
using MirrorSharp.Internal.Reflection;

namespace MirrorSharp.Extensions
{
    public class GenericLanguage<TSessionData> : ILanguage
    {
        private readonly Func<string, GenericLanguageSession<TSessionData>> createSession;

        public GenericLanguage(string name, Func<string, GenericLanguageSession<TSessionData>> createSession)
        {
            Name = name;
            this.createSession = createSession;
        }

        public string Name { get; }

        ILanguageSessionInternal ILanguage.CreateSession(string text) => createSession(text);
    }
}