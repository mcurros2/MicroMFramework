import { Group } from '@mantine/core';
import { Calendar, CalendarProps } from '@mantine/dates';
import dayjs from 'dayjs';
import { useState } from 'react';

function getDay(date: Date) {
    const day = date.getDay();
    return day === 0 ? 6 : day - 1;
}

function startOfWeek(date: Date) {
    return dayjs(new Date(date.getFullYear(), date.getMonth(), date.getDate() - getDay(date)))
        .startOf('day')
        .toDate();
}

function endOfWeek(date: Date) {
    return dayjs(new Date(date.getFullYear(), date.getMonth(), date.getDate() + (6 - getDay(date))))
        .endOf('day')
        .toDate();
}

function isInWeekRange(date: Date, value: Date | null) {
    if (!value) return false;
    const start = startOfWeek(value);
    const end = endOfWeek(value);
    return dayjs(date).isSame(start, 'day') || dayjs(date).isSame(end, 'day') ||
        (dayjs(date).isAfter(start) && dayjs(date).isBefore(end));
}

export interface WeekPickerProps extends CalendarProps {
    weekStartDatevalue: Date | null,
    setWeekStartValue: (value: Date | null) => void,
    setWeekEndValue?: (value: Date | null) => void,
    allowDeselect?: boolean,
}

export function WeekPicker(props: WeekPickerProps) {
    const {
        weekStartDatevalue, setWeekStartValue,
        setWeekEndValue, allowDeselect,
        ...others } = props;

    const [hovered, setHovered] = useState<Date | null>(null);

    return (
        <Group justify="center">
            <Calendar
                //TODO: desde migracion a Mantine v8 placeholder ya no existe
                //placeholder={undefined} onPointerEnterCapture={undefined} onPointerLeaveCapture={undefined}
                {...others}
                withCellSpacing={false}
                getDayProps={(date) => {
                    const currentDate = new Date(date);
                    const isHovered = isInWeekRange(currentDate, hovered);
                    const isSelected = isInWeekRange(currentDate, weekStartDatevalue);
                    const isInRange = isHovered || isSelected;

                    return {
                        onMouseEnter: () => setHovered(currentDate),
                        onMouseLeave: () => setHovered(null),
                        inRange: isInRange,
                        firstInRange: isInRange && currentDate.getDay() === 1,
                        lastInRange: isInRange && currentDate.getDay() === 0,
                        selected: isSelected,
                        onClick: () => {
                            if (allowDeselect && isSelected) {
                                setWeekStartValue(null);
                                if (setWeekEndValue) setWeekEndValue(null);
                            } else {
                                setWeekStartValue(startOfWeek(currentDate));
                                if (setWeekEndValue) setWeekEndValue(endOfWeek(currentDate));
                            }
                        },
                    };
                }} />
        </Group>
    );
}
