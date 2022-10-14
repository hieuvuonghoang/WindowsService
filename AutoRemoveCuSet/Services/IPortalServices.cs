using AutoRemoveCuSet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.Services
{
    public interface IPortalServices
    {
        Task<PortalResultToken> GeneratePortalTokeAsync();
        Task<bool> RemoveFeatureCuSet(CuSetType cuSetType, DateTime thoiGianCS, PortalResultToken accessToken);
    }
}
