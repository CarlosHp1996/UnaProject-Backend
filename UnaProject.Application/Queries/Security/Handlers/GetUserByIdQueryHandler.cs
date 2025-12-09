using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Security.Handlers
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdResponse>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<GetUserByIdResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            var result = new Result<GetUserByIdResponse>();

            try
            {
                var user = await _userRepository.GetUserById(query.Id, cancellationToken);

                if (user == null)
                {
                    result.WithError("User not found");
                    return result;
                }

                // Map to the response DTO
                var response = new GetUserByIdResponse
                {
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Cpf = user.Cpf,
                        Gender = user.Gender,
                        Addresses = user.Addresses?.Select(a => new AddressDto
                        {
                            Id = a.Id,
                            Street = a.Street,
                            CompletName = a.CompletName,
                            Number = a.Number,
                            Complement = a.Complement,
                            MainAddress = a.MainAddress,
                            Neighborhood = a.Neighborhood,
                            City = a.City,
                            State = a.State,
                            ZipCode = a.ZipCode
                        }).ToList()
                    }
                };

                result.Value = response;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
