using System;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.Utility
{
    public class DeadlineBeforeTournamentAttribute : ValidationAttribute
    {
        private readonly string _tournamentDateProperty;

        public DeadlineBeforeTournamentAttribute(string tournamentDateProperty)
        {
            _tournamentDateProperty = tournamentDateProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var deadlineDate = value as DateTime?;

            if (!deadlineDate.HasValue)
            {
                return ValidationResult.Success;
            }

            var propertyInfo = validationContext.ObjectType.GetProperty(_tournamentDateProperty);
            if (propertyInfo == null)
            {
                return new ValidationResult($"Property '{_tournamentDateProperty}' not found.");
            }

            var tournamentDate = propertyInfo.GetValue(validationContext.ObjectInstance) as DateTime?;

            if (!tournamentDate.HasValue)
            {
                return ValidationResult.Success;
            }

            if (deadlineDate > tournamentDate)
            {
                return new ValidationResult("Крайния срок не може да бъде след датата на турнира");
            }

            return ValidationResult.Success;
        }
    }
}