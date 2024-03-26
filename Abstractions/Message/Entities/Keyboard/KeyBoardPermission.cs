using Robin.Common;

namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardPermission(KeyboardPermissionType Type, EquatableImmutableArray<string>? RoleIds, EquatableImmutableArray<string>? UserIds);