import * as React from "react";
import { PositionInstance } from "@equinor/fusion";
import useSortedInstances from "./useSortedInstances";

type SortedInstances = {
    firstInstance: PositionInstance,
    lastInstance: PositionInstance | undefined,
};

export default (instances: PositionInstance[]): SortedInstances => {
    const { instancesByFrom, instancesByTo } = useSortedInstances(instances);

    const firstInstance = React.useMemo(() => instancesByFrom[0], [instancesByFrom]);
    const lastInstance = React.useMemo(() => instancesByTo.find(i => i.appliesTo.getTime), [
        instancesByTo,
    ]);
    return { firstInstance, lastInstance };
};