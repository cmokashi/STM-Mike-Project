using System.Data.Common;

namespace StaffingPocProj.Data;

public interface IDbConnectionFactory
{
    DbConnection Create();
}