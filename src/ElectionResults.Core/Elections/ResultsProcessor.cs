﻿using System.Collections.Generic;
using System.Linq;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;

namespace ElectionResults.Core.Elections
{
    public class ResultsProcessor
    {
        public static ElectionResultsResponse PopulateElectionResults(Turnout electionTurnout, Ballot ballot, List<CandidateResult> candidates, List<Party> parties)
        {
            ElectionResultsResponse results = new ElectionResultsResponse();
            results.NullVotes = electionTurnout.NullVotes;
            results.TotalVotes = electionTurnout.TotalVotes;
            results.ValidVotes = electionTurnout.ValidVotes;
            results.EligibleVoters = electionTurnout.EligibleVoters;
            results.TotalSeats = electionTurnout.TotalSeats;
            results.VotesByMail = electionTurnout.VotesByMail != 0 ? electionTurnout.VotesByMail : (int?)null;
            if (ballot.BallotType == BallotType.Referendum)
            {
                if (results.ValidVotes == 0)
                {
                    results.ValidVotes = results.TotalVotes - results.ValidVotes;
                }

                results.Candidates = new List<CandidateResponse>
                {
                    new CandidateResponse
                    {
                        Name = "DA",
                        ShortName = "DA",
                        Votes = candidates.FirstOrDefault().YesVotes,
                    },
                    new CandidateResponse
                    {
                        Name = "NU",
                        ShortName = "NU",
                        Votes = candidates.FirstOrDefault().NoVotes,
                    },
                    new CandidateResponse
                    {
                        Name = "NU AU VOTAT",
                        ShortName = "NU AU VOTAT",
                        Votes = (results.EligibleVoters - results.TotalVotes).GetValueOrDefault(),
                    }
                };
            }
            else
            {
                var colors = new List<string>();
                var logos = new List<string>();
                if (candidates == null)
                {
                    results.Candidates = new List<CandidateResponse>();
                    return results;
                }
                foreach (var candidate in candidates)
                {
                    var matchingParty = parties.GetMatchingParty(candidate.ShortName);
                    if (matchingParty != null)
                    {
                        colors.Add(matchingParty.Color);
                        logos.Add(matchingParty.LogoUrl);
                    }
                    else
                    {
                        colors.Add(null);
                        logos.Add(null);
                    }
                }
                results.Candidates = candidates.Select(c => new CandidateResponse
                {
                    ShortName = c.GetCandidateShortName(ballot),
                    Name = c.GetCandidateName(ballot),
                    Votes = c.Votes,
                    PartyColor = c.GetPartyColor(),
                    PartyLogo = c.Party?.LogoUrl,
                    Seats = c.TotalSeats,
                    SeatsGained = c.SeatsGained
                }).ToList();
                for (var i = 0; i < results.Candidates.Count; i++)
                {
                    var candidate = results.Candidates[i];
                    if (candidate.PartyColor.IsEmpty())
                    {
                        candidate.PartyColor = colors[i];
                    }
                    if (candidate.PartyLogo.IsEmpty())
                    {
                        candidate.PartyLogo = logos[i];
                    }
                }
            }

            results.Candidates = results.Candidates.OrderByDescending(c => c.Votes).ToList();
            if (ballot.BallotType == BallotType.Referendum)
            {
                results.Candidates = results.Candidates.OrderForReferendum(ballot.Election);
            }

            return results;
        }

    }
}
