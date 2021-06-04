using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JsonLogic;
using JsonLogic.Net;

/*
 * Sample data need to be in the below format
 * string changedParameter = "p_123" 
 * string jsonData =  @"{'p_593':'G1C','p_1764':'78UK','p_706':5,'p_725':4,'p_335':'D(CARSZ)'}";
 */
namespace RuleEngine
{
    public class Rule
    {
        static readonly HttpClient client = new HttpClient();
        private readonly JObject apiData = new JObject();

        public Rule()
        {
            client.BaseAddress = new Uri("https://uat.oc.com/api/");
            HttpResponseMessage response = client.GetAsync("iaaData?languageCode=en-gb&productid=1&shareUid=null&mode=null").Result;
            response.EnsureSuccessStatusCode();
            string result = response.Content.ReadAsStringAsync().Result;
            JObject json = JObject.Parse(result);
            this.apiData = JObject.Parse(result);
        }

        /*
         * Sample data need to be in the below format
         * string changedParameter = "p_123" 
         * string jsonData =  @"{'p_593':'G1C','p_1764':'78UK','p_706':5,'p_725':4,'p_335':'D(CARSZ)'}";
         */
        public string ExecuteRule(string changedParameter, string formJSON)
        {
            var jsonDataObject = JObject.Parse(formJSON);
            var parameters = this.apiData["data"]["parameters"];
            var conditions = this.apiData["data"]["conditions"];
            var rules = this.apiData["data"]["rules"];
            var propertyValueGroup = this.apiData["data"]["propertyValueGroup"];
            var parameter = parameters[changedParameter];

            var rulesList = parameter["rules"].ToList();

            for (int i = 0; i < rulesList.Count; i++)
            {
                var rule = rules[rulesList[i].ToString()];

                var internalCondtion = rule["internalCondition"];
                var selectionCondition = rule["selectionCondition"];

                var internalConditionJSONLogic = conditions[internalCondtion.ToString()];
                var selectionConditionJSONLogic = conditions[selectionCondition.ToString()];

                // Create an evaluator with default operators.
                var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);

                // Apply the rule to the data.
                object internalCondtionEvaluatorResult = evaluator.Apply(internalConditionJSONLogic, jsonDataObject);
                object selectionCondtionEvaluatorResult = evaluator.Apply(selectionConditionJSONLogic, jsonDataObject);

                if (internalCondtionEvaluatorResult.ToString() == "True" && selectionCondtionEvaluatorResult.ToString() == "True")
                {
                    if (rule["action"].ToString() == "updateNLock")
                    {
                        var pvgId = rule["propertyValueGroup"];
                        var pvg = propertyValueGroup[pvgId.ToString()];
                        var pvgList = pvg.ToList();
                        for (int j = 0; j < pvgList.Count; j++)
                        {
                            var pvgData = pvgList[j];
                            foreach (JToken attribute in pvgData)
                            {
                                JProperty jProperty = attribute.ToObject<JProperty>();
                                string propertyName = jProperty.Name;
                                if (propertyName.Contains("p_"))
                                {
                                    var parameterName = propertyName;
                                    var parameterValue = pvgData[parameterName].ToList()[0];
                                    jsonDataObject[parameterName] = parameterValue;
                                }
                            }
                        }
                    }
                }
            }
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonDataObject);
            return output;
        }
    }
}
