﻿using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MultipleAsyncInOne.Controllers {

    public class Car {

        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public float Price { get; set; }
    }

    public class CarsController : ApiController {

        public static readonly string[] PayloadSources = new[] { 
            "http://localhost:2700/api/cars/cheap",
            "http://localhost:2700/api/cars/expensive"
        };

        [HttpGet] // Synchronous and not In Parallel
        public IEnumerable<Car> AllCarsSync() {

            List<Car> carsResult = new List<Car>();
            foreach (var uri in PayloadSources) {

                IEnumerable<Car> cars = GetCars(uri);
                carsResult.AddRange(cars);
            }

            return carsResult;
        }

        [HttpGet] // Synchronous and In Parallel
        public IEnumerable<Car> AllCarsInParallelSync() {

            IEnumerable<Car> cars = PayloadSources.AsParallel()
                .SelectMany(uri => GetCars(uri)).AsEnumerable();

            return cars;
        }

        [HttpGet] // Asynchronous and not In Parallel
        public async Task<IEnumerable<Car>> AllCarsAsync() {

            List<Car> carsResult = new List<Car>();
            foreach (var uri in PayloadSources) {

                IEnumerable<Car> cars = await GetCarsAsync(uri);
                carsResult.AddRange(cars);
            }

            return carsResult;
        }

        [HttpGet] // Asynchronous and In Parallel (In a Blocking Fashion)
        public IEnumerable<Car> AllCarsInParallelBlockingAsync() {

            // NOTE: As we are using async/await keywords for our client async requests below,
            // it will use System.Threading.SynchronizationContext by default.
            // If we don't use ConfigureAwait(false), it will introduce deadlock here because
            // we are making a blocking call. More info:
            // http://www.tugberkugurlu.com/archive/asynchronousnet-client-libraries-for-your-http-api-and-awareness-of-async-await-s-bad-effects
            
            IEnumerable<Task<IEnumerable<Car>>> allTasks = PayloadSources.Select(uri => GetCarsAsync(uri));
            Task.WaitAll(allTasks.ToArray());

            return allTasks.SelectMany(task => task.Result);
        }

        [HttpGet] // Asynchronous and In Parallel (In a Non-Blocking Fashion)
        public async Task<IEnumerable<Car>> AllCarsInParallelNonBlockingAsync() {

            IEnumerable<Task<IEnumerable<Car>>> allTasks = PayloadSources.Select(uri => GetCarsAsync(uri));
            IEnumerable<Car>[] allResults = await Task.WhenAll(allTasks);

            return allResults.SelectMany(cars => cars);
        }

        // private helpers

        private async Task<IEnumerable<Car>> GetCarsAsync(string uri) {

            using (HttpClient client = new HttpClient()) {

                HttpResponseMessage response = await client.GetAsync(uri).ConfigureAwait(false);
                IEnumerable<Car> cars = await response.Content.ReadAsAsync<IEnumerable<Car>>().ConfigureAwait(false);

                return cars;
            }
        }

        private IEnumerable<Car> GetCars(string uri) {

            using (WebClient client = new WebClient()) {
                
                string carsJson = client.DownloadString(uri);
                IEnumerable<Car> cars = JsonConvert.DeserializeObject<IEnumerable<Car>>(carsJson);
                return cars;
            }
        }
    }
}