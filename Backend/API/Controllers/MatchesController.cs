using System.Runtime.Intrinsics.X86;
using API.Handlers;
using API.Hubs;
using API.Interfaces.Hubs;
using API.Models.Dtos;
using Domain;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers;

/// <summary>
/// Controller for matches
/// </summary>
/// <param name="matchRepository"></param>
/// <param name="userRepository"></param>
/// <param name="eloService"></param>
/// <param name="rankingHub"></param>
/// <param name="newsHub"></param>
[Route("[controller]/")]
public class MatchesController(
    IMatchRepository matchRepository,
    IUserRepository userRepository,
    IEloService eloService,
    IHubContext<RankingHub, IRankingHub> rankingHub,
    IHubContext<NewsHub, INewsHub> newsHub)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a match by its ID.
    /// </summary>
    /// <param name="id">The ID of the match to retrieve.</param>
    /// <returns>A match object if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{id:int}", Name = "GetMatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDto>> Get(int id)
    {
        var match = await matchRepository.Get(id);
        if (match == null)
        {
            return NotFound();
        }

        return (MatchDto)match;
    }


    /// <summary>
    /// Updates a match with the provided details.
    /// </summary>
    /// <param name="matchId">The id of the match to update</param>
    /// <param name="updateMatchDto">The updated info for the match</param>
    /// <returns>An updated match object if successful; otherwise, a 400 Bad Request response.</returns>
    [HttpPut("{matchId:int}", Name = "UpdateMatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MatchDto>> Update(int matchId, UpdateMatchDto updateMatchDto)
    {
        try
        {
            var winner = await userRepository.Get(updateMatchDto.WinnerId ?? 0);

            if (winner == null) return BadRequest();

            var match = await matchRepository.Get(matchId);
            if (match == null) return BadRequest();

            match.News = updateMatchDto.News;
            match.ExtraInfo1 = updateMatchDto.ExtraInfo1;
            match.ExtraInfo2 = updateMatchDto.ExtraInfo2;
            match.IsFinished = true;
            foreach (var matchPlayer in match.Players)
            {
                matchPlayer.IsWinner = matchPlayer.User.Id == updateMatchDto.WinnerId;
                matchPlayer.Score = updateMatchDto.Scores.First(s => s.PlayerId == matchPlayer.User.Id).Score;
            }
            
            var updatedMatch = await matchRepository.Update(match);

            if (updateMatchDto.UpdateWinner)
            {
                await eloService.AdjustEloForMatch(match);
                await rankingHub.Clients.All.NotifyAboutUpdatedRanking();
            }

            // Update news
            var latestMatches = await matchRepository.GetLatestWithNews(5);
            var news = latestMatches.Select(m => new NewsDto { News = m.News ?? "", Date = m.Date ?? DateTime.Now })
                .ToList();
            await newsHub.Clients.All.UpdatedNews(news.ToList());
            var matchDto = (MatchDto)updatedMatch;
            return Ok(matchDto);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Retrieves all matches.
    /// </summary>
    /// <returns>A list of all matches.</returns>
    [HttpGet(Name = "GetAllMatches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<MatchDto>> GetAll()
    {
        return (await matchRepository.GetAll()).Select(match => (MatchDto)match);
    }

    /// <summary>
    /// Adds a new match.
    /// </summary>
    /// <param name="player1Id">Player 1 id</param>
    /// <param name="player2Id">Player 2 id</param>
    /// <param name="numberOfSets">The number of sets</param>
    /// <returns>A 201 Created response if the match is successfully added; otherwise, a 400 Bad Request response.</returns>
    [HttpPost(Name = "CreateMatch")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create(int player1Id, int player2Id, NumberOfSets numberOfSets)
    {
        try
        {
            var match = await matchRepository.Create(player1Id, player2Id, numberOfSets);
            return Created($"/match/{match}", match);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Deletes a match by its ID.
    /// </summary>
    /// <param name="id">The ID of the match to delete.</param>
    /// <returns>A 200 OK response if the match is successfully deleted.</returns>
    [HttpDelete(Name = "DeleteMatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Delete(int id)
    {
        await matchRepository.Delete(id);
        return Ok();
    }
}