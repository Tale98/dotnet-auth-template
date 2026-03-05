namespace Api.Models.Exceptions;

public class UnauthorizedException : Exception 
{ 
    public UnauthorizedException(string message = "User is inactive") : base(message) { }
}