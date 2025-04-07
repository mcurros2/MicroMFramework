import { useEffect, useState } from "react";
import { ClickedRegionData, RegionSelector, SelectedRegionData } from "../../src";

export function RegionSelectorTest() {
    const [selectedRegions, setSelectedRegions] = useState<Record<string, SelectedRegionData>>({});
    const [clickedRegions, setClickedRegions] = useState<Record<string, ClickedRegionData>>({});

    useEffect(() => {
        console.log('Selected regions', selectedRegions);
    }, [selectedRegions]);

    return (
        <RegionSelector
            mapId={"cf25f06ae9fc59cd"}
            countries={['US']}
            mapCenter={{ lat: 39.8097343, lng: -98.5556199 }}
            selectedRegions={selectedRegions}
            setSelectedRegions={setSelectedRegions}
            clickedRegions={clickedRegions}
            setClickedRegions={setClickedRegions}
        />
    )
}