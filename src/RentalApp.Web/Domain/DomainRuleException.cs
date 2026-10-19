namespace RentalApp.Web.Domain;

// Thrown when an action breaks a business rule (wrong status, unit already leased, ...).
// Controllers catch it and show the message to the user. It is never a bug.
public class DomainRuleException(string message) : Exception(message);
