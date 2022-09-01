using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class SecondOpinionAnswered : INotification
    {
        public SecondOpinionAnswered(QuerySecondOpinion secondOpinion, QuerySecondOpinionResponse response)
        {
            SecondOpinion = secondOpinion;
            Response = response;
        }

        public QuerySecondOpinion SecondOpinion { get; }
        public QuerySecondOpinionResponse Response { get; }
    }
}
