using Content.Server.Database._Misfits.CivicPoints;
using Content.Server.Database._Misfits.Experience;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database._Misfits;

public static class MisfitsDatabaseModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        CharacterExperienceDatabaseModel.Configure(modelBuilder);
        CivicPointsDatabaseModel.Configure(modelBuilder);
    }
}
