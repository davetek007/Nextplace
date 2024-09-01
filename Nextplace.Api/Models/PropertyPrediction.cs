﻿using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PropertyPrediction(string minerHotKey, string minerColdKey, DateTime predictionDate, double predictedSalePrice, DateTime predictedSaleDate, List<MinerStats> minerStats)
{
    [Required]
    public string MinerHotKey { get; } = minerHotKey;

    [Required]
    public string MinerColdKey { get; } = minerColdKey;

    [Required]
    public DateTime PredictionDate { get; } = predictionDate;

    [Required]
    public double PredictedSalePrice { get; } = predictedSalePrice;

    [Required]
    public DateTime PredictedSaleDate { get; } = predictedSaleDate;

    public List<MinerStats>? MinerStats { get; } = minerStats;

    public Property? Property { get; set; } 
}