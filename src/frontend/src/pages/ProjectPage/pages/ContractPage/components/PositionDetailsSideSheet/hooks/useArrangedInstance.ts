import { useMemo } from "react";
import { PositionInstance } from "@equinor/fusion";
import useSortedInstances from "./useSortedInstances";

type SortedInstances = {
    firstInstance: PositionInstance,
    lastInstance: PositionInstance | undefined,
};

export default (instances: PositionInstance[]): SortedInstances => {
    const { instancesByFrom, instancesByTo } = useSortedInstances(instances);

    const firstInstance = useMemo(() => instancesByFrom[0], [instancesByFrom]);
    const lastInstance = useMemo(() => instancesByTo.find(i => i.appliesTo.getTime), [
        instancesByTo,
    ]);
    return { firstInstance, lastInstance };
};