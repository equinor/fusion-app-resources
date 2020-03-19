using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class RejectRequestRequest
    {
        public string Reason { get; set; } = null!;



        #region Validator

        public class Validator : AbstractValidator<RejectRequestRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason must be provided when rejecting request").WithName("reason");
            }
        }

        #endregion
    }

}
