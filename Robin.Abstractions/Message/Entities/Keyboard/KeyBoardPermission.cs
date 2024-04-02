namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardPermission(KeyboardPermissionType Type, List<string>? RoleIds = null, List<string>? UserIds = null);