
import { ReportingPath, Spinner, ErrorMessage } from '@equinor/fusion-components';
import OrgChartCard, { OrgChartCardType } from '../components/OrgChartCard';
import {
    useComponentDisplayType,
    useApiClients,
    Position,
    PositionReportPath,
    useCurrentContext,
} from '@equinor/fusion';
import { getParentPositionId, getInstances, isInstanceFuture, isInstancePast } from '../../../../../orgHelpers';
import useArrangedInstance from '../hooks/useArrangedInstance';
import { FC, useState, useCallback, useEffect, useMemo } from 'react';

type ReportingPathTabProps = {
    selectedPosition: Position;
    filterToDate: Date;
};

const ReportingPathTab: FC<ReportingPathTabProps> = ({ selectedPosition, filterToDate }) => {
    const [isFetching, setIsFetching] = useState<boolean>(false);
    const [error, setError] = useState<Error | null>();
    const [reportingPath, setReportingPath] = useState<PositionReportPath | null>(null);
    const apiClients = useApiClients();
    const project = useCurrentContext();

    
    const { firstInstance, lastInstance } = useArrangedInstance(selectedPosition?.instances || []);

    const getReportPathDate = useCallback(() => {
        if (!selectedPosition || selectedPosition.instances.length <= 0) {
            return filterToDate;
        }
        const isPast = lastInstance && isInstancePast(lastInstance, filterToDate);
        const isFuture = isInstanceFuture(firstInstance, filterToDate);

        if (lastInstance && isPast) {
            return lastInstance.appliesTo;
        }
        if (isFuture) {
            return firstInstance.appliesFrom;
        }
        
        return filterToDate;
    }, [selectedPosition, firstInstance, lastInstance, filterToDate]);


    const fetchReportPathAsync = async (positionId: string, projectId: string) => {
        setError(null);
        setReportingPath(null);
        setIsFetching(true);
        try {
            const date = getReportPathDate();
            const isoDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000) //Converts to local date
                .toISOString()
                .slice(0, 10);

            const response = await apiClients.org.getPositionReportPathAsync(
                projectId,
                positionId,
                isoDate
            );
            setReportingPath(response.data);
        } catch (error) {
            setError(error);
        }

        setIsFetching(false);
    };

    const componentDisplayType = useComponentDisplayType();
    const cardHeight = componentDisplayType === 'Compact' ? 132 : 142;
    const rowMargin = componentDisplayType === 'Compact' ? 120 : 134;
    const cardMargin = componentDisplayType === 'Compact' ? 16 : 16;

    useEffect(() => {
        if (selectedPosition && !error && !isFetching && project?.externalId) {
            fetchReportPathAsync(selectedPosition.id, project.externalId);
        }
    }, [selectedPosition]);

    const getDetailedStructure = useCallback(
        (
            structure: Position[],
            linked?: boolean,
            parentPosition?: Position
        ): OrgChartCardType[] => {
            return structure.map(position => {
                return {
                    position,
                    instance: getInstances(position, filterToDate)[0],
                    id: linked
                        ? `${position.id} - ${(parentPosition && parentPosition.id) || ''}`
                        : position.id,
                    parentId: parentPosition
                        ? parentPosition.id
                        : getParentPositionId(position, filterToDate),
                    aside: false,
                    linked: linked,
                };
            });
        },
        [filterToDate]
    );

    const reportingPathStructure = useMemo((): OrgChartCardType[] => {
        if (!selectedPosition || !reportingPath) {
            return [];
        }
        const reportsTo = reportingPath.path;
        if (!reportsTo) {
            return getDetailedStructure([selectedPosition]);
        }
        const reportsToPositions = reportingPath.reportPositions
            .slice(0)
            .reverse()
            .map(reportingTo => reportingTo.position);
        const reportsToStructure = getDetailedStructure(
            [...reportsToPositions, selectedPosition],
            false
        );
        const taskOwners = reportingPath.taskOwners;

        if (!taskOwners) {
            return reportsToStructure;
        }

        const taskOwnerStructure = getDetailedStructure(taskOwners, true, selectedPosition);

        return [...reportsToStructure, ...taskOwnerStructure];
    }, [selectedPosition, reportingPath]);

    if (isFetching) {
        return <Spinner centered />;
    }
    if (error) {
        return (
            <ErrorMessage
                title="An error occurred while fetching data"
                errorType="error"
                hasError
                action="Try again"
                message="Could not fetch reports to data"
                onTakeAction={() =>
                    selectedPosition &&
                    project?.externalId &&
                    fetchReportPathAsync(selectedPosition.id, project.externalId)
                }
            />
        );
    }
    if (reportingPath === null) {
        return null;
    }
    if (!selectedPosition || (reportingPathStructure.length <= 0 && !isFetching)) {
        return <ErrorMessage title="No reporting data" errorType="noData" hasError />;
    }

    return (
        <ReportingPath
            structure={reportingPathStructure}
            component={OrgChartCard}
            cardHeight={cardHeight}
            cardWidth={340}
            cardMargin={cardMargin}
            rowMargin={rowMargin}
        />
    );
};
export default ReportingPathTab;
