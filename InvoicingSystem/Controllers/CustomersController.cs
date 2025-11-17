using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoicingSystem.Data;
using InvoicingSystem.Models;
using InvoicingSystem.DTOs;

namespace InvoicingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ApplicationDbContext context, ILogger<CustomersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Customer>>> CreateCustomer([FromBody] CreateCustomerDto createCustomerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<Customer>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.SelectMany(x => x.Value!.Errors).Select(x => x.ErrorMessage).ToList()
                    });
                }

                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == createCustomerDto.Email);
                if (existingCustomer != null)
                {
                    return BadRequest(new ApiResponse<Customer>
                    {
                        Success = false,
                        Message = "A customer with this email already exists"
                    });
                }

                var customer = new Customer
                {
                    FirstName = createCustomerDto.FirstName,
                    LastName = createCustomerDto.LastName,
                    Email = createCustomerDto.Email,
                    Phone = createCustomerDto.Phone,
                    BillingAddress = createCustomerDto.BillingAddress,
                    City = createCustomerDto.City,
                    State = createCustomerDto.State,
                    ZipCode = createCustomerDto.ZipCode,
                    Country = createCustomerDto.Country,
                    ShippingAddress = createCustomerDto.ShippingAddress,
                    ShippingCity = createCustomerDto.ShippingCity,
                    ShippingState = createCustomerDto.ShippingState,
                    ShippingZipCode = createCustomerDto.ShippingZipCode,
                    ShippingCountry = createCustomerDto.ShippingCountry
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} created successfully", customer.CustomerId);

                return Ok(new ApiResponse<Customer>
                {
                    Success = true,
                    Message = "Customer created successfully",
                    Data = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, new ApiResponse<Customer>
                {
                    Success = false,
                    Message = "An error occurred while creating the customer"
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Customer>>>> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();

                return Ok(new ApiResponse<List<Customer>>
                {
                    Success = true,
                    Message = "Customers retrieved successfully",
                    Data = customers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return StatusCode(500, new ApiResponse<List<Customer>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers"
                });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Customer>>> GetCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                {
                    return NotFound(new ApiResponse<Customer>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                return Ok(new ApiResponse<Customer>
                {
                    Success = true,
                    Message = "Customer retrieved successfully",
                    Data = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                return StatusCode(500, new ApiResponse<Customer>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the customer"
                });
            }
        }

       
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<Customer>>> UpdateCustomer(int id, [FromBody] CreateCustomerDto updateCustomerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<Customer>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.SelectMany(x => x.Value!.Errors).Select(x => x.ErrorMessage).ToList()
                    });
                }

                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound(new ApiResponse<Customer>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                if (customer.Email != updateCustomerDto.Email)
                {
                    var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == updateCustomerDto.Email);
                    if (existingCustomer != null)
                    {
                        return BadRequest(new ApiResponse<Customer>
                        {
                            Success = false,
                            Message = "A customer with this email already exists"
                        });
                    }
                }

                
                customer.FirstName = updateCustomerDto.FirstName;
                customer.LastName = updateCustomerDto.LastName;
                customer.Email = updateCustomerDto.Email;
                customer.Phone = updateCustomerDto.Phone;
                customer.BillingAddress = updateCustomerDto.BillingAddress;
                customer.City = updateCustomerDto.City;
                customer.State = updateCustomerDto.State;
                customer.ZipCode = updateCustomerDto.ZipCode;
                customer.Country = updateCustomerDto.Country;
                customer.ShippingAddress = updateCustomerDto.ShippingAddress;
                customer.ShippingCity = updateCustomerDto.ShippingCity;
                customer.ShippingState = updateCustomerDto.ShippingState;
                customer.ShippingZipCode = updateCustomerDto.ShippingZipCode;
                customer.ShippingCountry = updateCustomerDto.ShippingCountry;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} updated successfully", customer.CustomerId);

                return Ok(new ApiResponse<Customer>
                {
                    Success = true,
                    Message = "Customer updated successfully",
                    Data = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                return StatusCode(500, new ApiResponse<Customer>
                {
                    Success = false,
                    Message = "An error occurred while updating the customer"
                });
            }
        }
    }
}