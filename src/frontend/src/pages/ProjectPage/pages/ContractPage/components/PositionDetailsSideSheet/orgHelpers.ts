import { Position, PositionInstance } from '@equinor/fusion';

export const isInstanceFuture = (instance: PositionInstance, filterToDate: Date) =>
    instance.appliesFrom.getTime() > filterToDate.getTime();

export const isInstancePast = (instance: PositionInstance, filterToDate: Date) =>
    instance.appliesTo.getTime() < filterToDate.getTime();

export const sortInstancesByFrom = (instances: PositionInstance[]) =>
    [...instances].sort((a, b) => a.appliesFrom.getTime() - b.appliesFrom.getTime());

export const sortInstancesByTo = (instances: PositionInstance[]) =>
    [...instances].sort((a, b) => b.appliesTo.getTime() - a.appliesTo.getTime());

export const filterInstancesByDate = (instances: PositionInstance[], date: Date) =>
    instances.filter(
        instance =>
            date.getTime() >= instance.appliesFrom.getTime() &&
            date.getTime() <= instance.appliesTo.getTime()
    );

export const getInstances = (position: Position, filterToDate: Date) => {
    const filteredInstance = filterInstancesByDate(position.instances, filterToDate);

    if (filteredInstance.length > 0) {
        return filteredInstance;
    }
    const firstInstance = sortInstancesByFrom(position.instances)[0];
    if (isInstanceFuture(firstInstance, filterToDate)) {
        const firstInstances = filterInstancesByDate(position.instances, firstInstance.appliesFrom);
        return firstInstances;
    }
    const instancesByTo = sortInstancesByTo(position.instances);
    const lastInstance = instancesByTo[instancesByTo.length - 1];
    if (isInstancePast(lastInstance, filterToDate)) {
        const lastInstances = filterInstancesByDate(position.instances, lastInstance.appliesTo);
        return lastInstances;
    }
    return [];
};

export const getReportsToIds = (position: Position, filterToDate: Date) =>
    getInstances(position, filterToDate)[0].reportsToIds;

export const getTaskOwnersIds = (position: Position, filterToDate: Date) =>
    getInstances(position, filterToDate)[0].taskOwnerIds || undefined;

export const getParentPositionId = (position: Position, filterToDate: Date) => {
    const instances = getInstances(position, filterToDate);
    return instances.length > 0 ? instances[0].parentPositionId || undefined : undefined;
};
