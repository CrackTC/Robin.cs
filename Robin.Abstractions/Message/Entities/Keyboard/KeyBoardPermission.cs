namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardPermission(KeyboardPermissionType Type, List<string>? RoleIds, List<string>? UserIds);