using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using MP.Core.Common.Heplers;
using MP.Scraping.GameProcessing;
using MP.Scraping.Models.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace MP.Scraping.Common.ServiceScripts
{
    public static class ScriptLoader
    {
        const string SCRIPT_ASSEMBLY_NAME = "GameServicesScripts";
        const string LIBRARY_FILE = "GameServicesScripts.dll";
        const string CONST_SERVICE_VARIABLES_CLASS_TYPE = "MP.ScriptDebugger.Scripts.Common.ImmutableServicesValues";

        private static WeakReference _assemmblyWeakRef;
        private static Assembly _scriptAssembly;
        private static AssemblyLoadContext _scriptAssemblyContext;

        private static object _libraryFileLocker = new object();
        private static object _compilerLocker = new object();

        private static bool _isScriptsCompiling;

        public static bool CompileScripts()
        {
            lock (_compilerLocker)
            {
                if (_isScriptsCompiling)
                    return false;

                _isScriptsCompiling = true;
            }

            if (!Directory.Exists("Scripts"))
            {
                lock (_compilerLocker)
                {
                    _isScriptsCompiling = false;
                    throw new DirectoryNotFoundException("Directory \"Scripts\" must be present");
                }
            }

            List<SyntaxTree> syntaxTreesList = new List<SyntaxTree>();

            try
            {
                foreach (string filePath in Directory.GetFiles("Scripts", "*.cs", SearchOption.AllDirectories))
                {
                    FileHelper.WhaitFileUnlock(filePath);
                    string code = File.ReadAllText(filePath);
                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                    syntaxTreesList.Add(syntaxTree);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Script reading error");

                lock (_compilerLocker)
                {
                    _isScriptsCompiling = false;
                    return false;
                }
            }

            if (syntaxTreesList.Count == 0)
            {
                lock (_compilerLocker)
                {
                    _isScriptsCompiling = false;
                    return false;
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                SCRIPT_ASSEMBLY_NAME,
                syntaxTreesList,
                GetReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug)
            );

            using (var memoryStream = new MemoryStream())
            {
                EmitResult compilResult = compilation.Emit(memoryStream);
                if (!compilResult.Success)
                {
                    string errors = "Scripts was not compiled";

                    foreach (var error in compilResult.Diagnostics)
                        errors += "\n" + error;

                    Log.Error(errors);
                }
                else
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    lock (_libraryFileLocker)
                    {
                        using (var fileStream = new FileStream(LIBRARY_FILE, FileMode.OpenOrCreate))
                        {
                            memoryStream.CopyTo(fileStream);
                        }
                    }

                    Log.Information("Scripts was compiled successfully");
                }

                lock (_compilerLocker)
                {
                    _isScriptsCompiling = false;
                    return compilResult.Success;
                }
            }
        }

        public static bool ScriptValidationCheck(Assembly scriptsAssembly)
        {
            Type immutableServicesValues = scriptsAssembly.GetType(CONST_SERVICE_VARIABLES_CLASS_TYPE);
            if (immutableServicesValues == null)
                throw new ScriptValidationException($"Service Scripts must contain {CONST_SERVICE_VARIABLES_CLASS_TYPE} class");


            Type baseScriptsType = typeof(GameService);
            Type codeAttributeType = typeof(ShortCode);

            foreach (var scriptType in scriptsAssembly.ExportedTypes.Where(i => i.BaseType == baseScriptsType))
            {
                var codeAttribute = scriptType.GetCustomAttribute<ShortCode>();
                if (codeAttribute == null)
                    throw new ScriptValidationException($"\"ShortCode\" Attribute was not found in the {scriptType.FullName}");

                string serviceValuesFieldName = $"{codeAttribute.ServiceCode}Values";
                var serviceValues = immutableServicesValues.GetField(serviceValuesFieldName);
                if (serviceValues == null)
                    throw new ScriptValidationException($"{immutableServicesValues.Name} does not contain {serviceValuesFieldName} field");
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LoadAssembly()
        {
            if (!IsAssemblyUnloaded())
                throw new FileLoadException();

            _scriptAssemblyContext = new AssemblyLoadContext("scriptAssemblyContext", true);
            _assemmblyWeakRef = new WeakReference(_scriptAssemblyContext);

            lock (_libraryFileLocker)
            {
                using (var fs = File.Open(LIBRARY_FILE, FileMode.Open))
                    _scriptAssembly = _scriptAssemblyContext.LoadFromStream(fs);
            }

            _scriptAssemblyContext.Unloading += (context) =>
            {
                _scriptAssemblyContext = null;
                _scriptAssembly = null;
            };

            SetConstantDataToServiceTable(_scriptAssembly);
        }

        /// <summary>
        /// Выгружает текущую загруженную сборку скриптов.
        /// Перед выгрузкой сборки убедитесь, что все ссылки на сборку были удалены, и ни один из скриптов сборки сейчас не запущен
        /// </summary>
        public static void UnloadAssembly()
        {
            if (_scriptAssemblyContext == null)
                throw new Exception("Script assembly not loaded");

            _scriptAssemblyContext.Unload();

            for (int i = 0; _assemmblyWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (_assemmblyWeakRef.IsAlive)
                Log.Error("Assembly was not unloaded");

            _assemmblyWeakRef = null;
        }

        public static Type GetTypeFromScriptAssembly(string type) => _scriptAssembly?.GetType(type);

        private static IEnumerable<MetadataReference> GetReferences()
        {
            List<MetadataReference> references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GameProcessing.GameService).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AngleSharp.Configuration).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Core.Contexts.GService).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonConverter).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Xml.XmlAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.TypeConverter).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ObjectModel").Location),
            };

            IEnumerable<MetadataReference> coreReferences = Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(i => MetadataReference.CreateFromFile(Assembly.Load(i).Location));

            references.AddRange(coreReferences);

            return references;
        }

        private static bool IsAssemblyUnloaded()
            => _assemmblyWeakRef == null && _scriptAssemblyContext == null && _scriptAssembly == null;

        private static void SetConstantDataToServiceTable(Assembly scriptsAssembly)
        {
            ScriptValidationCheck(scriptsAssembly);

            Type immutableServicesValues = scriptsAssembly.GetType(CONST_SERVICE_VARIABLES_CLASS_TYPE);

            using (ServiceContext context = new ServiceContext())
            {
                var servicesInDB = context.Services.Include(i => i.SupportedCountries);

                foreach (var service in servicesInDB)
                {
                    string serviceConstantsFieldName = $"{service.Code}Values";
                    ServiceConstants serviceConstants = immutableServicesValues
                        .GetField(serviceConstantsFieldName)
                        .GetValue(null)
                        as ServiceConstants;

                    string[] serviceCountriesCodesInDB = service.SupportedCountries.Select(i => i.CountryCode).ToArray();
                    List<ServiceCountry> supportedCountriesToAdd = serviceConstants.SupportedCountries
                        .Where(i => !serviceCountriesCodesInDB.Contains(i.CountryCode))
                        .Select(i => new ServiceCountry { CountryCode = i.CountryCode, CurrencyList = i.CurrencyCodes, LanguageList = i.LanguagesCodes, ServiceCode = service.Code })
                        .ToList();

                    string[] countriesCodesInContstants = serviceConstants.SupportedCountries.Select(i => i.CountryCode).ToArray();
                    List<ServiceCountry> supportedCountriesToDelete = service.SupportedCountries
                        .Where(i => !countriesCodesInContstants.Contains(i.CountryCode))
                        .ToList();

                    foreach (var serviceCountry in service.SupportedCountries)
                    {
                        if (supportedCountriesToDelete.Contains(serviceCountry))
                            continue;

                        var supportedCountryConts = serviceConstants.SupportedCountries.FirstOrDefault(i => i.CountryCode == serviceCountry.CountryCode);
                        serviceCountry.CurrencyList = supportedCountryConts.CurrencyCodes;
                        serviceCountry.LanguageList = supportedCountryConts.LanguagesCodes;
                    }
                    context.ServiceCountries.AddRange(supportedCountriesToAdd);
                    context.ServiceCountries.RemoveRange(supportedCountriesToDelete);
                }

                context.SaveChanges();
            }
        }
    }
}
