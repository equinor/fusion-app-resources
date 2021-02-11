
import { ModalSideSheet, Button, AddIcon, EditIcon } from '@equinor/fusion-components';
import styles from '../styles.less';
import { Position } from '@equinor/fusion';
import Contract from '../../../../../models/contract';
import ExternalPositionSidesheet from './ExternalPositionSidesheet';
import { FC, useState, useCallback } from 'react';

type CreateOrEditExternalPositionButtonProps = {
    contract: Contract;
    onComplete: (position: Position) => void;
    repType: 'company-rep' | 'contract-responsible';
    existingPosition: Position | null;
};

const CreateOrEditExternalPositionButton: FC<CreateOrEditExternalPositionButtonProps> = ({
    repType,
    contract,
    onComplete,
    existingPosition,
}) => {
    const [isShowing, setIsShowing] = useState(false);

    const show = useCallback(() => setIsShowing(true), []);
    const onClose = useCallback(() => {
        setIsShowing(false);
    }, []);

    return (
        <>
            {existingPosition ? (
                <div className={styles.helpText}>
                    <Button frameless onClick={show}>
                        <EditIcon />{' '}
                        <span className={styles.buttonPositionName}>
                            Edit {existingPosition.name}
                        </span>
                    </Button>
                </div>
            ) : (
                <div className={styles.helpText}>
                    <span>If you can't find your position, try to </span>
                    <Button frameless onClick={show}>
                        <AddIcon /> Add new position
                    </Button>
                </div>
            )}
            <ModalSideSheet
                show={isShowing}
                onClose={onClose}
                size="large"
                header={
                    existingPosition
                        ? `Edit ${existingPosition.name}`
                        : 'Add new position to contract'
                }
            >
                <ExternalPositionSidesheet
                    contract={contract}
                    existingPosition={existingPosition}
                    onClose={onClose}
                    onComplete={onComplete}
                    repType={repType}
                />
            </ModalSideSheet>
        </>
    );
};

export default CreateOrEditExternalPositionButton;
