import * as React from 'react';
import * as styles from './styles.less';
import CertifyToPicker from '../../../CertifiyToPicker';
import { Button, SyncIcon, Spinner } from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../../../appContext';
import { useContractContext } from '../../../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';

type CertifyToPopoverProps = {
    canEdit: boolean;
    admins: PersonDelegation[];
};

const CertifyToPopover: React.FC<CertifyToPopoverProps> = ({ canEdit, admins }) => {
    const [selectedDate, setSelectedDate] = React.useState<Date | null>(null);

    const [isReCertifying, setIsReCertifying] = React.useState<boolean>(false);
    const [reCertificationError, setReCertificationError] = React.useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const { dispatchContractAction, contract } = useContractContext();
    const currentContext = useCurrentContext();
    console.log(contract)

    const reCertifyAdminsAsync = React.useCallback(
        async (projectId: string, contractId: string, date: Date) => {
            setIsReCertifying(false);
            setReCertificationError(null);

            try {
                const requests = admins.map((a) =>
                    apiClient.reCertifyRoleDelegationAsync(projectId, contractId, a.id, date)
                );
                const response = await Promise.all(requests);
                dispatchContractAction({
                    collection: 'administrators',
                    verb: 'merge',
                    payload: response,
                });
            } catch (e) {
                setReCertificationError(e);
            } finally {
                setIsReCertifying(false);
            }
        },
        [admins, apiClient]
    );

    const onReCertifyClick = React.useCallback(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        console.log(contractId , projectId , selectedDate , canEdit)

        if (contractId && projectId && selectedDate && canEdit) {
            reCertifyAdminsAsync(projectId, contractId, selectedDate);
        }
    }, [currentContext, contract, canEdit, reCertifyAdminsAsync, selectedDate]);

    return (
        <div className={styles.container}>
            <CertifyToPicker
                onChange={setSelectedDate}
                defaultSelected="12-months"
                isReCertification
            />
            <div className={styles.certifyButtonContainer}>
                <Button disabled={!canEdit} onClick={onReCertifyClick}>
                    <div className={styles.syncButton}>
                        {isReCertifying ? (
                            <Spinner inline />
                        ) : (
                            <>
                                <SyncIcon />
                                <span>Re-Certify</span>
                            </>
                        )}
                    </div>
                </Button>
            </div>
        </div>
    );
};

export default CertifyToPopover;
