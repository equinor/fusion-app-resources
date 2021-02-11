

import * as styles from '../styles.less';
import {
    IconButton,
    ArrowBackIcon,
    Button,
    Stepper,
    SkeletonBar,
    Step,
    styling,
} from '@equinor/fusion-components';
import classNames from 'classnames';
import { FC } from 'react';

type ContractWizardSkeletonProps = {
    isEdit: boolean;
    onGoBack: () => void;
};

const ContractWizardSkeleton: FC<ContractWizardSkeletonProps> = ({ isEdit, onGoBack }) => {
    return (
        <div className={styles.container}>
            <header className={styles.header}>
                <IconButton onClick={onGoBack}>
                    <ArrowBackIcon />
                </IconButton>
                <h2>
                    {isEdit ? 'Edit ' : ''} <SkeletonBar />
                </h2>
                <Button outlined disabled>
                    Cancel
                </Button>
                <Button outlined disabled>
                    Save
                </Button>
            </header>
            <Stepper activeStepKey={isEdit ? 'contract-details' : 'select-contract'}>
                <Step title="Select contract" stepKey="select-contract" disabled>
                    <div className={styles.stepContainer}>
                        <SkeletonBar />
                    </div>
                </Step>
                <Step title="Contract details" stepKey="contract-details" disabled>
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={classNames(styles.field, styles.big)}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={classNames(styles.field, styles.big)}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                            <div className={styles.field}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                            <div className={styles.field}>
                                <SkeletonBar width="100%" height={styling.grid(7)} />
                            </div>
                        </div>
                        <div className={styles.actions}>
                            {!isEdit && (
                                <Button outlined disabled>
                                    Previous
                                </Button>
                            )}
                            <Button disabled>Next</Button>
                        </div>
                    </div>
                </Step>
                <Step title="External" stepKey="externals" disabled>
                    <div className={styles.stepContainer}>
                        <SkeletonBar />
                    </div>
                </Step>
            </Stepper>
        </div>
    );
};

export default ContractWizardSkeleton;
