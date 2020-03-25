import * as React from "react";
import { PositionInstance } from "@equinor/fusion";
import { sortInstancesByFrom, sortInstancesByTo } from "../orgHelpers";

type SortedInstances = {
    instancesByFrom: PositionInstance[],
    instancesByTo: PositionInstance[],
};

export default (instances: PositionInstance[]): SortedInstances => {
    const instancesByFrom = React.useMemo(() => sortInstancesByFrom(instances), [instances]);
    const instancesByTo = React.useMemo(() => sortInstancesByTo(instances), [instances]);

    return { instancesByFrom, instancesByTo };
};
