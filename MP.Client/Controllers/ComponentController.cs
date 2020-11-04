using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.JsonResponses;
using MP.Client.ComponentModels.Common;
using MP.Client.Contexts;
using MP.Client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace MP.Client.Controllers
{
    [Route("[controller]/{name}")]
    public class ComponentController : Controller
    {
        [FromRoute]
        public string Name { get; set; }

        private IDbConnection _connection { get; }

        [FromQuery]
        public string CountryCode { get; set; } = "RU";
        [FromQuery]
        public string CurrencyCode { get; set; } = "RUB";

        private const string QUERY_ONE_COMPONET = @"SELECT * FROM ""Components"" WHERE ""Name"" = '{0}'";
        private const string QUERY_MULTIPLE_COMPONENTS = @"SELECT * FROM ""Components"" WHERE ""Name"" in ('{0}')";

        private readonly string[] _homeComponetsPartOne = new string[] { "carousel", "newDiscounts", "services", "serviceGroupedGames" };
        private readonly string[] _homeComponetsPartTwo = new string[] { "tags", "freeGames", "newGames" };

        private static string _componentModelNamespace;
        private static MethodInfo _deserializationMethod;

        static ComponentController()
        {
            _componentModelNamespace = typeof(IComponentModel).Namespace.Replace(".Common", String.Empty) + '.';
            
            _deserializationMethod = typeof(JsonConvert)
                .GetMethods()
                .FirstOrDefault(i => i.Name == nameof(JsonConvert.DeserializeObject) && i.IsGenericMethod);
        }

        public ComponentController(MainContext context)
        {
            _connection = context.Database.GetDbConnection();
        }

        public IActionResult Index()
        {
            if (Name == "home")
            {
                return CreateHomeComponent();
            }

            return new NotFoundResult();
        }

        IActionResult CreateHomeComponent()
        {
            string part = Request.Query["section"];


            if (part == "firstPart" || part == "secondPart")
            {
                Dictionary<string, object> answer = new Dictionary<string, object>();

                string[] componentsNames;
                switch (part)
                {
                    case "secondPart": componentsNames = _homeComponetsPartTwo; break;
                    default: componentsNames = _homeComponetsPartOne; break;
                }

                string query = String.Format(QUERY_MULTIPLE_COMPONENTS, String.Join("','", componentsNames));
                IEnumerable<Component> components = _connection.Query<Component>(query);

                foreach (Component com in components)
                {
                    object obj = CreateComponent(com);
                    answer.Add(com.Name, obj);
                }

                return new JsonResult(answer);
            }

            return new JsonErrorResult("Unknown section", $"Section with name {part} not found");
        }

        object CreateComponent(Component component)
        {
            string fullTypeName = _componentModelNamespace + component.Model;
            Type modelType = Type.GetType(fullTypeName);
            var method = _deserializationMethod.MakeGenericMethod(modelType);
            var resultObject = method.Invoke(null, new object[] { component.Data });

            if (!(resultObject is IComponentModel))
                throw new InvalidCastException($"Model must implement the {nameof(IComponentModel)} interface");
            
            ComponentModelOptions options 
                = new ComponentModelOptions(_connection, component.Title, CountryCode, CurrencyCode);

            return (resultObject as IComponentModel).CreateResponseObject(options);
        }
    }
}
