namespace WhMgr.Utilities
{
    using System;
    using System.IO;

    using HandlebarsDotNet;
    using HandlebarsDotNet.Helpers;

    public static class Renderer
    {
        private static readonly IHandlebars _handlebarsContext = Handlebars.Create();

        static Renderer()
        {
            HandlebarsHelpers.Register(_handlebarsContext);
        }

        public static string Parse(string text, dynamic model)
        {
            var template = _handlebarsContext.Compile(text);
            return template(model);
        }

        public static string ParseFile(string path, dynamic model)
        {
            var templateData = ReadData(path); // REVIEW: Replace with File.ReadAllText?
            //_handlebarsContext.RegisterTemplate(path, templateData);
            return Parse(templateData, model);
        }

        private static string ReadData(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Template does not exist at path: {path}", path);
            }
            using (var sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }
    }
}