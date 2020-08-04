using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Helpers;
using Ganedata.Core.Models;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Ganedata.Core.Entities.Enums;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Ganedata.Core.Entities.Domain.Models;
using System.Web.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WMS.Controllers.WebAPI
{
    public class ApiCurrencyExRateController : BaseApiController
    {
        private readonly ITenantsCurrencyRateServices _tenantCurrencyRateServices;
        private readonly IActivityServices _activityServices;
        private readonly ITenantLocationServices _tenantLocationServices;
        private readonly ITenantsServices _tenantServices;

        public ApiCurrencyExRateController(ITerminalServices terminalServices, ITenantLocationServices tenantLocationServices, IOrderService orderService, IProductServices productServices, IUserService userService, ITenantsCurrencyRateServices TenantCurrencyRateServices, ITenantsServices TenantServices)
            : base(terminalServices, tenantLocationServices, orderService, productServices, userService)
        {
            _tenantCurrencyRateServices = TenantCurrencyRateServices;
            _tenantServices = TenantServices;
        }


        public async Task<IHttpActionResult> GetTenantCurrencyExRate(int tenantId)
        {
            try
            {
                if (tenantId == 0)
                {
                    return BadRequest("Tenant id cannot be 0");
                }
                var tenantBaseCurrency = _tenantCurrencyRateServices.GetTenantCurrencyById(tenantId);
                                var model = _tenantCurrencyRateServices.GetTenantCurrencies(tenantId).ToList();
                CurrenciesRates currenciesRates = new CurrenciesRates();
                currenciesRates = await GetCurrencyExchangeRate(tenantBaseCurrency.CurrencyName, model);
                if (!currenciesRates.success)
                {
                    return NotFound();
                }
                else
                {
                    List<TenantCurrenciesExRates> tenantCurrenciesExRatesList = new List<TenantCurrenciesExRates>();
                    var tenant = _tenantServices.GetByClientId((int)tenantId);
                    foreach (var item in currenciesRates.quotes)
                    {
                        foreach (var itemModel in model)
                        {
                            string Key = item.Key;
                            Key = Key.Substring(3);
                           string currency1 = (itemModel.GlobalCurrency.CurrencyName).Substring(0,3).ToUpper();

                            if (Key == currency1)
                            {
                                TenantCurrenciesExRates tenantCurrenciesExRates = new TenantCurrenciesExRates();
                                tenantCurrenciesExRates.TenantCurrencyID = itemModel.CurrencyID;
                                tenantCurrenciesExRates.ActualRate = Convert.ToDecimal(item.Value);
                                tenantCurrenciesExRates.DiffFactor = Convert.ToDecimal(itemModel.DiffFactor);
                                tenantCurrenciesExRates.Rate = Convert.ToDecimal(tenantCurrenciesExRates.DiffFactor + tenantCurrenciesExRates.ActualRate);
                                tenantCurrenciesExRates.DateUpdated = DateTime.UtcNow;
                                SaveTenantCurrencyRate(tenantCurrenciesExRates);
                            }
                        }
                    }
                    return Ok("Currency rate saved");
                }
            }
            catch (Exception Exp)
            {
                if (Exp.InnerException != null)
                {
                    _tenantCurrencyRateServices.LogAPI(Exp.InnerException.ToString(), HttpStatusCode.Ambiguous, typeof(ApiCurrencyExRateController));
                    Debug.WriteLine(Exp.InnerException);
                }
                return BadRequest();
            }
        }

        public async Task<CurrenciesRates> GetCurrencyExchangeRate(string BaseCurrency, List<TenantCurrencies> modellist)
        {
            HttpResponseMessage response = null;
            try
            {
                //base currency currently fixed as USD as apilayer only returns results from USD after subscription we can change it to 
                BaseCurrency = "usd";
                CurrenciesRates model = new CurrenciesRates();
                string apiUrl;
                if (modellist != null && modellist.Count > 0)
                {
                    List<string> namesList = new List<string>();
                    foreach (var ml in modellist)
                    {
                        namesList.Add((ml.GlobalCurrency.CurrencyName).ToUpper().Substring(0, 3));
                    }
                    //Api Key Provided by apilayer
                    apiUrl = "http://www.apilayer.net/api/live?source=" + (BaseCurrency).ToUpper()
                                            + "&access_key=30c90be532e151b17ba2130cfa081063&currencies=" +
                                            string.Join(",", namesList);
                }
                else
                {
                    apiUrl = "http://www.apilayer.net/api/live?source=" + (BaseCurrency).ToUpper() + "&access_key=12aedfc3e200141c180ba037bb11a2f7";
                }
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(new Uri(apiUrl));
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        _tenantCurrencyRateServices.LogAPI(response.Content.ReadAsStringAsync().Result, HttpStatusCode.OK, typeof(ApiCurrencyExRateController));
                        model = JsonConvert.DeserializeObject<CurrenciesRates>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        _tenantCurrencyRateServices.LogAPI(response.Content.ReadAsStringAsync().Result, (HttpStatusCode)response.StatusCode, typeof(ApiCurrencyExRateController));
                    }
                }
                return model;
            }
            catch (Exception Exp)
            {
                if (Exp.InnerException != null)
                {
                    _tenantCurrencyRateServices.LogAPI(Exp.InnerException.ToString(), HttpStatusCode.Ambiguous, typeof(ApiCurrencyExRateController));
                    Debug.WriteLine(Exp.InnerException);
                }
                return null;
            }
        }
        public void SaveTenantCurrencyRate([Bind(Include = "ExchnageRateID,TenantCurrencyID,DiffFactor,ActualRate,Rate,DateUpdated,Tenant_TenantId")]  TenantCurrenciesExRates tenantCurrenciesExRates)
        {
            if (ModelState.IsValid)
            {
                _tenantCurrencyRateServices.Insert(tenantCurrenciesExRates);
            }
        }

    }
}
