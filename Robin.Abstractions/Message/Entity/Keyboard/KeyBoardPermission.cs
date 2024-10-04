namespace Robin.Abstractions.Message.Entity.Keyboard;

public record KeyboardPermission(
    KeyboardPermissionType Type,
    List<string>? RoleIds = null,
    List<string>? UserIds = null);
