import * as React from 'react';
import { useContractContext } from '../../../../../../contractContex';
import * as styles from './styles.less';
import { SkeletonBar, PositionCard, SkeletonDisc } from '@equinor/fusion-components';
import { formatDate, Position } from '@equinor/fusion';
import Contract from '../../../../../../models/contract';

const createFieldWithSkeleton = (
    name: string,
    render: (contract: Contract) => React.ReactNode,
    renderSkeleton?: () => React.ReactNode
) => {
    const contractContext = useContractContext();

    return (
        <div className={styles.field}>
            <label>{name}</label>
            <div className={styles.value}>
                {contractContext.isFetchingContract ? (
                    renderSkeleton ? (
                        renderSkeleton()
                    ) : (
                        <SkeletonBar />
                    )
                ) : contractContext.contract ? (
                    render(contractContext.contract)
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

const renderPosition = (position: Position | null) =>
    position ? (
        <PositionCard
            position={position}
            showDate
            showExternalId
            showLocation
            showObs
            showTimeline
            showRotation
        />
    ) : (
        'N/A'
    );

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
        'EquinorContractRep',
        contract => renderPosition(contract.externalContractResponsible),
        () => <PositionCardSkeleton />
    );

const ContractDetailsPage = () => {
    return (
        <div className={styles.container}>
            <div className={styles.row}>
                <ContractNumber />
                <Contractor />
            </div>
            <div className={styles.row}>
                <FromDate />
                <ToDate />
            </div>
            <div className={styles.row}>
                <EquinorContractResponsible />
                <EquinorCompanyRep />
            </div>
            <div className={styles.row}>
                <ExternalCompanyRep />
                <ExternalContractResponsible />
            </div>
        </div>
    );
};

export default ContractDetailsPage;
