import * as React from 'react';
import { useContractContext } from '../../../../../../contractContex';
import * as styles from './styles.less';
import {
    SkeletonBar,
    PositionCard,
    SkeletonDisc,
    IconButton,
    EditIcon,
    useTooltipRef,
    HelpIcon,
} from '@equinor/fusion-components';
import { formatDate, Position, useHistory, useCurrentContext } from '@equinor/fusion';
import Contract from '../../../../../../models/contract';
import { getInstances, isInstanceFuture, isInstancePast } from '../../../../orgHelpers';
import { Link } from 'react-router-dom';

const createFieldWithSkeleton = (
    name: string,
    render: (contract: Contract) => React.ReactNode,
    renderSkeleton?: () => React.ReactNode
) => {
    const { isFetchingContract, contract } = useContractContext();

    return (
        <div className={styles.field}>
            <label>{name}</label>
            <div className={styles.value}>
                {isFetchingContract && !contract ? (
                    renderSkeleton ? (
                        renderSkeleton()
                    ) : (
                        <SkeletonBar />
                    )
                ) : contract ? (
                    render(contract)
                ) : null}
            </div>
        </div>
    );
};

const ContractNumber = () =>
    createFieldWithSkeleton('Contract number', contract => contract.contractNumber);
const Contractor = () =>
    createFieldWithSkeleton('Contractor', contract => contract.company?.name || null);
const FromDate = () =>
    createFieldWithSkeleton('From date', contract =>
        contract.startDate ? formatDate(contract.startDate) : 'N/A'
    );
const ToDate = () =>
    createFieldWithSkeleton('To date', contract =>
        contract.endDate ? formatDate(contract.endDate) : 'N/A'
    );

const PositionCardSkeleton = () => (
    <div className={styles.positionCardSkeleton}>
        <SkeletonDisc size="medium" />
        <div className={styles.content}>
            <SkeletonBar />
            <SkeletonBar />
        </div>
    </div>
);

const renderPosition = (position: Position | null) => {
    if (!position) {
        return 'N/A';
    }
    const filterToDate = React.useMemo(() => new Date(), []);
    const instance = React.useMemo(() => getInstances(position, filterToDate)[0], [
        position,
        filterToDate,
    ]);
    const isFuture = React.useMemo(() => isInstanceFuture(instance, filterToDate), [
        position,
        filterToDate,
    ]);
    const isPast = React.useMemo(() => isInstancePast(instance, filterToDate), [
        position,
        filterToDate,
    ]);
    return (
        <PositionCard
            position={position}
            instance={instance}
            showDate
            showExternalId
            showLocation
            showObs
            showTimeline
            showRotation
            isFuture={isFuture}
            isPast={isPast}
        />
    );
};

const EquinorContractResponsible = () =>
    createFieldWithSkeleton(
        'Equinor contract responsible',
        contract => renderPosition(contract.contractResponsible),
        () => <PositionCardSkeleton />
    );
const EquinorCompanyRep = () =>
    createFieldWithSkeleton(
        'Equinor company rep',
        contract => renderPosition(contract.companyRep),
        () => <PositionCardSkeleton />
    );
const ExternalCompanyRep = () =>
    createFieldWithSkeleton(
        'External company rep',
        contract => renderPosition(contract.externalCompanyRep),
        () => <PositionCardSkeleton />
    );
const ExternalContractResponsible = () =>
    createFieldWithSkeleton(
        'External contract responsible',
        contract => renderPosition(contract.externalContractResponsible),
        () => <PositionCardSkeleton />
    );

const ContractDetailsPage = () => {
    const editTooltipRef = useTooltipRef('Edit contract', 'left');
    const helpIconRef = useTooltipRef('help page', 'left');

    const history = useHistory();
    const contractContext = useContractContext();
    const currentContext = useCurrentContext();

    return (
        <div className={styles.container}>
            <div className={styles.contractDetails}>
                <div className={styles.row}>
                    <ContractNumber />
                    <Contractor />
                </div>
                <div className={styles.row}>
                    <FromDate />
                    <ToDate />
                </div>
                <div className={styles.row}>
                    <EquinorCompanyRep />
                    <EquinorContractResponsible />
                </div>
                <div className={styles.row}>
                    <ExternalCompanyRep />
                    <ExternalContractResponsible />
                </div>
            </div>
            <div className={styles.aside}>
                <IconButton
                    ref={editTooltipRef}
                    onClick={() =>
                        history.push(`/${currentContext?.id}/${contractContext.contract?.id}/edit`)
                    }
                >
                    <EditIcon />
                </IconButton>
                <Link target="_blank" to="/help?responsibilities">
                    <IconButton ref={helpIconRef}>
                        <HelpIcon />
                    </IconButton>
                </Link>
            </div>
        </div>
    );
};

export default ContractDetailsPage;
