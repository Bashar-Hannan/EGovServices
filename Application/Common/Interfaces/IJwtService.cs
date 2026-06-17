using EGovServices.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGovServices.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
