using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Tables;

namespace OWSManagement.Requests.Users
{
    public class GetUsersRequest
    {
        private readonly Guid _customerGuid;

        public GetUsersRequest(Guid customerGuid)
        {
            _customerGuid = customerGuid;
        }
        public async Task<List<User>> Handle()
        {
            await Task.Delay(1);

            return new List<User>();
        }
    }
}
