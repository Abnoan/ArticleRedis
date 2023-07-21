using ArticleRedis.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ArticleRedis.Controllers
{
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string CountriesKey = "Countries";
        private const string RestCountriesUrl = "https://restcountries.eu/rest/v2/all";

        public CountriesController(IDistributedCache distributedCache, IHttpClientFactory httpClientFactory)
        {
            _distributedCache = distributedCache;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var countriesObject = await _distributedCache.GetStringAsync(CountriesKey);

            if (!string.IsNullOrWhiteSpace(countriesObject))
            {
                return Ok(countriesObject);
            }

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(RestCountriesUrl);
            var responseData = await response.Content.ReadAsStringAsync();

            var countries = JsonConvert.DeserializeObject<List<Country>>(responseData);

            var memoryCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3600),
                SlidingExpiration = TimeSpan.FromSeconds(1200)
            };

            await _distributedCache.SetStringAsync(CountriesKey, responseData, memoryCacheEntryOptions);

            return Ok(countries);
        }
    }
}
