
import { CheckBox } from '@equinor/fusion-components';
import { forwardRef, PropsWithChildren, MutableRefObject } from 'react';
import * as styles from './styles.less';

export type SelectionCellProps = {
  isSelected: boolean;
  onChange: () => void;
  isHovering?: boolean;
  indeterminate?: boolean;
};

const SelectionCell = forwardRef<
  HTMLDivElement | null,
  PropsWithChildren<SelectionCellProps>
>(
  (
    {
      isSelected,
      onChange,
      indeterminate,
    },
    ref
  ) => {
    return (
      <div
        className={styles.cell}
        ref={ref as MutableRefObject<HTMLDivElement | null>}
      >
        <CheckBox
          selected={isSelected}
          onChange={onChange}
          indeterminate={indeterminate}
        />
      </div>
    );
  }
);

export default SelectionCell;
