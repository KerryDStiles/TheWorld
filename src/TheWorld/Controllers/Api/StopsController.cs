using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheWorld.Models;
using TheWorld.Service;
using TheWorld.ViewModels;

namespace TheWorld.Controllers.Api
{
    [Route("/api/trips/{tripName}/stops")]
    public class StopsController : Controller
    {
        private GeoCoordsService _coordsServices;
        private ILogger<StopsController> _logger;
        private IWorldRepository _repository;

        public StopsController(IWorldRepository repository, 
            ILogger<StopsController> logger,
            GeoCoordsService coordsServices)
        {
            _repository = repository;
            _logger = logger;
            _coordsServices = coordsServices;
        }

        [HttpGet("")]
        public IActionResult Get(string tripName)
        {
            try
            {
                var trip = _repository.GetTripByName(tripName);

                return Ok(Mapper.Map<IEnumerable<StopViewModel>>(trip.Stops.OrderBy(s => s.Order).ToList()));
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get stopes: {0}", ex);
            }

            return BadRequest("Failed to get stopes");
        }

        [HttpPost("")]
        public async Task<IActionResult> Post(string tripName, [FromBody]StopViewModel vm)
        {
            try
            {
                // If VM is valid
                if (ModelState.IsValid)
                {
                    var newStop = Mapper.Map<Stop>(vm);

                    // Lookup the Geocodes
                    var result = await _coordsServices.GetCoordsAsync(newStop.Name);
                    if (!result.Success)
                    {
                        _logger.LogError(result.Message);
                    }
                    else
                    {
                        newStop.Latitude = result.Latitude;
                        newStop.Longitude = result.Longitude;

                        //Save to the Database
                        _repository.AddStop(tripName, newStop);

                        if (await _repository.SaveChangesAsync())
                        {
                            return Created($"/api/trip/{tripName}/stops/{newStop.Name}",
                                Mapper.Map<StopViewModel>(newStop));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("Failed to save new Stop: {0}", ex);
            }

            return BadRequest("Failed to save new stop");
        }
    }
}
