using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Helpers;
using UnaProject.Infra.Data;

namespace UnaProject.Infra.Repositories
{
    public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public UserRepository(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<CreateUserResponse> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
        {

            try
            {
                // Basic email validation
                var validEmail = await _emailService.IsValidEmailAsync(request.Email);
                if (!validEmail)
                    throw new Exception("Invalid Email!");


                // CPF validation (if provided)
                if (!string.IsNullOrWhiteSpace(request.Cpf) && !ValidationHelpers.IsValidCpf(request.Cpf))
                    throw new Exception("Invalid CPF!");


                // Address validation (if provided)
                if (request.Addresses != null)
                {
                    foreach (var addressDto in request.Addresses)
                    {
                        if (!ValidationHelpers.IsValidCep(addressDto.ZipCode))
                            throw new Exception($"The CEP '{addressDto.ZipCode}' for street '{addressDto.Street}' is invalid.");
                    }
                }

                // Remove spaces from the name and generate a new UserName.
                string userName = UserNameHelper.GenerateUserName(request.Name);

                var applicationUser = new ApplicationUser
                {
                    UserName = userName,
                    Email = request.Email.ToLower().Trim(),
                    PhoneNumber = request.PhoneNumber,
                    Cpf = request.Cpf,
                    Gender = request.Gender
                };

                if (request.Addresses != null && request.Addresses.Any())
                {
                    foreach (var addressDto in request.Addresses)
                    {
                        applicationUser.Addresses.Add(new Address
                        {
                            Street = addressDto.Street,
                            CompletName = addressDto.CompletName,
                            City = addressDto.City,
                            State = (Domain.Enums.EnumState)addressDto.State,
                            ZipCode = addressDto.ZipCode,
                            Neighborhood = addressDto.Neighborhood,
                            Number = addressDto.Number,
                            Complement = addressDto.Complement,
                            MainAddress = (bool)addressDto.MainAddress
                        });
                    }
                }

                var identityResult = await _userManager.CreateAsync(applicationUser, request.Password);

                if (!identityResult.Succeeded)
                    throw new Exception("Error!");


                var userDto = new Application.Models.Dtos.UserDto
                {
                    Id = applicationUser.Id,
                    UserName = applicationUser.UserName,
                    Email = applicationUser.Email,
                    PhoneNumber = applicationUser.PhoneNumber,
                    Cpf = applicationUser.Cpf,
                    Gender = applicationUser.Gender,
                    Addresses = applicationUser.Addresses.Select(a => new Application.Models.Dtos.AddressDto
                    {
                        Id = a.Id,
                        Street = a.Street,
                        CompletName = a.CompletName,
                        City = a.City,
                        State = a.State,
                        ZipCode = a.ZipCode,
                        Neighborhood = a.Neighborhood,
                        Number = a.Number,
                        Complement = a.Complement,
                        MainAddress = a.MainAddress
                    }).ToList()
                };

                var response = new CreateUserResponse
                {
                    User = userDto,
                    Message = "User created successfully"
                };

                await _emailService.SendEmailConfirmationAsync(applicationUser.Email);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = new Result<UpdateUserResponse>();
                ApplicationUser user;

                // If it's a password recovery, search for the user by email.
                if (request.IsPasswordRecovery && !string.IsNullOrWhiteSpace(request.Email))
                {
                    user = await _context.Users
                                         .Include(u => u.Addresses)
                                         .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower().Trim(), cancellationToken);

                    if (user == null)
                        throw new Exception("User not found with this email.");
                }
                else
                {
                    // Busca tradicional por ID
                    user = await _context.Users
                                         .Include(u => u.Addresses)
                                         .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

                    if (user == null)
                        throw new Exception("User not found.");
                }

                // Step 1: Validate Inputs
                if (!string.IsNullOrWhiteSpace(request.Cpf) && !ValidationHelpers.IsValidCpf(request.Cpf))
                    throw new Exception("The provided CPF is invalid!");

                if (request.Addresses != null)
                {
                    foreach (var addressDto in request.Addresses)
                    {
                        if (!ValidationHelpers.IsValidCep(addressDto.ZipCode))
                            throw new Exception($"The CEP '{addressDto.ZipCode}' is invalid.");
                    }
                }

                // Step 3: Update User Properties (apenas se não for recuperação de senha)
                if (!request.IsPasswordRecovery)
                {
                    user.UserName = request.Name ?? user.UserName;
                    user.Email = request.Email?.ToLower().Trim() ?? user.Email;
                    user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
                    user.Cpf = request.Cpf ?? user.Cpf;
                    user.Gender = request.Gender ?? user.Gender;

                    // Step 4: Synchronize Addresses
                    if (request.Addresses != null)
                    {
                        var addressesFromRequest = request.Addresses.ToDictionary(a => a.Id);
                        var addressesInDb = user.Addresses.ToList();

                        // Remove addresses that are in DB but not in the request
                        foreach (var addressInDb in addressesInDb)
                        {
                            if (!addressesFromRequest.ContainsKey(addressInDb.Id))
                                user.Addresses.Remove(addressInDb);                            
                        }

                        foreach (var addressDto in request.Addresses)
                        {
                            if (addressDto.Id == Guid.Empty)
                            {
                                // Add new address
                                user.Addresses.Add(new Address
                                {
                                    Street = addressDto.Street,
                                    CompletName = addressDto.CompletName,
                                    City = addressDto.City,
                                    State = (Domain.Enums.EnumState)addressDto.State,
                                    ZipCode = addressDto.ZipCode,
                                    Neighborhood = addressDto.Neighborhood,
                                    Number = addressDto.Number,
                                    Complement = addressDto.Complement,
                                    MainAddress = (bool)addressDto.MainAddress
                                });
                            }
                            else
                            {
                                // Update existing address
                                var existingAddress = user.Addresses.FirstOrDefault(a => a.Id == addressDto.Id);
                                if (existingAddress != null)
                                {
                                    existingAddress.Street = addressDto.Street;
                                    existingAddress.CompletName = addressDto.CompletName;
                                    existingAddress.City = addressDto.City;
                                    existingAddress.State = (Domain.Enums.EnumState)addressDto.State;
                                    existingAddress.ZipCode = addressDto.ZipCode;
                                    existingAddress.Neighborhood = addressDto.Neighborhood;
                                    existingAddress.Number = addressDto.Number;
                                    existingAddress.Complement = addressDto.Complement;
                                    existingAddress.MainAddress = (bool)addressDto.MainAddress;
                                }
                            }
                        }
                    }

                    // Remove spaces from the name and generate a new UserName
                    string userName = UserNameHelper.GenerateUserName(request.Name);
                    user.UserName = userName;

                    // Step 5: Save User Changes (only if it's not a password recovery)
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                        throw new Exception(updateResult.Errors.First().Description);
                }

                // Step 6: Update Password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetPasswordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);

                    if (!resetPasswordResult.Succeeded)
                        throw new Exception(resetPasswordResult.Errors.First().Description);
                }

                // Step 7: Prepare Response
                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Cpf = user.Cpf,
                    Gender = user.Gender,
                    Addresses = user.Addresses.Select(a => new AddressDto
                    {
                        Id = a.Id,
                        Street = a.Street,
                        CompletName = a.CompletName,
                        City = a.City,
                        State = a.State,
                        ZipCode = a.ZipCode,
                        Neighborhood = a.Neighborhood,
                        Number = a.Number,
                        Complement = a.Complement,
                        MainAddress = a.MainAddress
                    }).ToList()
                };

                var response = new UpdateUserResponse
                {
                    User = userDto,
                    Message = request.IsPasswordRecovery ? "Password changed successfully" : "User updated successfully"
                };

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

                if (user == null)
                    return false;

                // Remove associated addresses if necessary
                if (user.Addresses != null && user.Addresses.Any())
                {
                    _context.Addresses.RemoveRange(user.Addresses);
                }

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                    throw new Exception(result.Errors.FirstOrDefault()?.Description ?? "Failed to delete user.");

                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<ApplicationUser> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _context.Users
                                     .Include(u => u.Addresses)
                                     .Where(u => u.Id == id)
                                     .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                    throw new Exception("User not found.");

                return user;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<AsyncOutResult<IEnumerable<ApplicationUser>, int>> GetUsers(GetUsersRequestFilter filter, CancellationToken cancellationToken)
        {
            try
            {
                string sortBy = filter.SortBy ?? "Name";
                bool ascending = filter.SortDirection?.ToLower() != "desc";

                // Get users queryable  
                var usersQuery = _userManager.Users.AsQueryable();

                // Apply search filter if provided  
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    string searchTerm = filter.SearchTerm.ToLower();
                    usersQuery = usersQuery.Where(u =>
                        u.UserName.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))
                    );
                }

                // Apply sorting  
                if (!string.IsNullOrEmpty(sortBy))
                {
                    // Default to ascending order if not specified  

                    switch (sortBy.ToLower())
                    {
                        case "name":
                            usersQuery = ascending ?
                                usersQuery.OrderBy(u => u.UserName) :
                                usersQuery.OrderByDescending(u => u.UserName);
                            break;
                        case "email":
                            usersQuery = ascending ?
                                usersQuery.OrderBy(u => u.Email) :
                                usersQuery.OrderByDescending(u => u.Email);
                            break;
                        case "phonenumber":
                            usersQuery = ascending ?
                                usersQuery.OrderBy(u => u.PhoneNumber) :
                                usersQuery.OrderByDescending(u => u.PhoneNumber);
                            break;
                        default:
                            usersQuery = ascending ?
                                usersQuery.OrderBy(u => u.Id) :
                                usersQuery.OrderByDescending(u => u.Id);
                            break;
                    }
                }
                else
                    usersQuery = usersQuery.OrderBy(u => u.Id);


                // Get total count  
                int totalCount = await usersQuery.CountAsync(cancellationToken);

                // Apply pagination  
                int page = filter.Page ?? 1;
                int pageSize = filter.PageSize ?? 10;
                int skip = (page - 1) * pageSize;

                var users = await usersQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Calculate total pages  
                int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Create response  
                var response = new GetAllUsersResponse
                {
                    Users = users.Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber
                    }),
                    TotalCount = totalCount,
                    PageCount = pageCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return new AsyncOutResult<IEnumerable<ApplicationUser>, int>(users, totalCount);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
