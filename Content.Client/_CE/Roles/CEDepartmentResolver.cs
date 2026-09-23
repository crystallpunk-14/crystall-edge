using Content.Shared._CE.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Roles;

/// <summary>
/// Resolves the department a job/secret role belongs to for display purposes (color + name),
/// mirroring CECharacterUIController's own department-matching logic. Shared by
/// CEGhostTargetWindow (table row coloring/search) and CECharacterViewWindow (header faction
/// label) so both stay in sync instead of drifting apart with copy-pasted loops.
/// </summary>
public static class CEDepartmentResolver
{
    /// <summary>
    /// Finds the department that determines a job's display color: the last match, unless a
    /// Primary department is found first (SS14's "primary department" convention), in which case
    /// that one locks in and later matches no longer affect the result. Also returns the raw ID +
    /// localized name of every matching department (not just the winning one) for search purposes.
    /// </summary>
    public static (DepartmentPrototype? Department, string SearchText) ResolveJobDepartment(
        IPrototypeManager prototypes,
        ProtoId<JobPrototype> jobId)
    {
        DepartmentPrototype? matched = null;
        var parts = new List<string>();

        foreach (var department in prototypes.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!department.Roles.Contains(jobId))
                continue;

            parts.Add(department.ID);
            parts.Add(Loc.GetString(department.Name));

            if (matched is not { Primary: true })
                matched = department;
        }

        return (matched, string.Join(' ', parts));
    }

    /// <summary>
    /// Same as <see cref="ResolveJobDepartment"/> but for secret roles/departments, which have no
    /// Primary concept - first match wins for the display color.
    /// </summary>
    public static (CESecretDepartmentPrototype? Department, string SearchText) ResolveSecretDepartment(
        IPrototypeManager prototypes,
        ProtoId<CESecretRolePrototype> roleId)
    {
        CESecretDepartmentPrototype? matched = null;
        var parts = new List<string>();

        foreach (var department in prototypes.EnumeratePrototypes<CESecretDepartmentPrototype>())
        {
            if (!department.Roles.Contains(roleId))
                continue;

            parts.Add(department.ID);
            parts.Add(Loc.GetString(department.Name));

            matched ??= department;
        }

        return (matched, string.Join(' ', parts));
    }
}
