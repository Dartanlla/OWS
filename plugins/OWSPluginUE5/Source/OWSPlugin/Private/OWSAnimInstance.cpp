// Copyright 2021 Sabre Dart Studios


#include "OWSAnimInstance.h"

float UOWSAnimInstance::GetStartTimeByDistance(UAnimSequence* AnimSequence, float distance)
{
    FRawCurveTracks CurvesOfAnim = AnimSequence->GetCurveData();
    TArray<FFloatCurve> Curves = CurvesOfAnim.FloatCurves;
    FFloatCurve DistanceCurve, SpeedCurve;

    for (int i = 0; i < Curves.Num(); i++)
    {
        if (Curves[i].Name.DisplayName == "DistanceCurve")
        {
            DistanceCurve = Curves[i];
        }
        else if (Curves[i].Name.DisplayName == "Speed") {
            SpeedCurve = Curves[i];
        }
    }

    int keys = DistanceCurve.FloatCurve.GetNumKeys();
    float min; float max; DistanceCurve.FloatCurve.GetValueRange(min, max);
    if (keys > 1) {
        if (distance > max) {
            return DistanceCurve.FloatCurve.Keys[keys - 1].Time;
        }
        else if (distance < min) {
            return DistanceCurve.FloatCurve.Keys[0].Time;
        }
        else {

            for (int x = 1; x < keys - 1; x++) {
                if (DistanceCurve.FloatCurve.Keys[x].Value > distance) {

                    //Exact Value (linear)
                    float deltaDist, deltaTime, pct;
                    deltaDist = DistanceCurve.FloatCurve.Keys[x].Value - DistanceCurve.FloatCurve.Keys[x - 1].Value;
                    deltaTime = DistanceCurve.FloatCurve.Keys[x].Time - DistanceCurve.FloatCurve.Keys[x - 1].Time;
                    pct = (distance - DistanceCurve.FloatCurve.Keys[x - 1].Value) / deltaDist;

                    return DistanceCurve.FloatCurve.Keys[x - 1].Time + deltaTime * pct;
                }
            }
        }
    }

    return 0;
}